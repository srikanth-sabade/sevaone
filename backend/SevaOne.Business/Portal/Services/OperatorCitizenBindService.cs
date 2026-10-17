using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

/// <summary>
/// Assisted-mode "bind a citizen" flow: an operator sends an OTP to the citizen standing at the
/// counter, the citizen reads it out, the operator enters it, and the citizen is then bound to
/// the operator's own OperatorSession (ActiveCitizenId) for the rest of that session -- so a
/// subsequent Seva Sindhu apply resolves the citizen's SevaOneUserId server-side (see
/// CurrentApplicantResolver) instead of trusting anything the client sends.
///
/// Deliberately separate from PortalAuthService's citizen mobile-OTP login: that flow signs the
/// citizen in (issues WSO2 tokens); this one never does -- the citizen isn't present at a
/// keyboard, the operator is acting on their behalf. Shares only the low-level OTP mechanics
/// (ISevaSessionStore for the challenge, ISmsOtpSender for delivery, PortalAuthService.HashOtp).
/// </summary>
public sealed class OperatorCitizenBindService : IOperatorCitizenBindService
{
    private static readonly Regex MobileFormat = new("^[0-9]{10,15}$");

    private readonly ISevaSessionStore _sessions;
    private readonly ISmsOtpSender _smsOtp;
    private readonly IPortalRepository _portal;
    private readonly IOperatorSessionStore _operatorSessions;
    private readonly IAuditRepository _audit;
    private readonly ICurrentOperator _current;
    private readonly IClock _clock;

    public OperatorCitizenBindService(
        ISevaSessionStore sessions,
        ISmsOtpSender smsOtp,
        IPortalRepository portal,
        IOperatorSessionStore operatorSessions,
        IAuditRepository audit,
        ICurrentOperator current,
        IClock clock)
    {
        _sessions = sessions;
        _smsOtp = smsOtp;
        _portal = portal;
        _operatorSessions = operatorSessions;
        _audit = audit;
        _current = current;
        _clock = clock;
    }

    public async Task<SendOperatorCitizenOtpResult> SendOtpAsync(SendOperatorCitizenOtpRequest request, CancellationToken ct)
    {
        RequireOperator();

        var mobile = (request.Mobile ?? "").Trim();
        if (!MobileFormat.IsMatch(mobile))
        {
            throw new DomainException("Enter a valid mobile number.");
        }

        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var challengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        await _sessions.SaveLoginStateAsync(
            challengeId,
            new LoginState { PendingMobile = mobile, OtpHash = PortalAuthService.HashOtp(otp) },
            TimeSpan.FromMinutes(10),
            ct);

        await _smsOtp.SendOtpAsync(mobile, otp, ct);

        return new SendOperatorCitizenOtpResult(challengeId);
    }

    public async Task<BoundCitizenDto> VerifyAndBindAsync(VerifyOperatorCitizenOtpRequest request, CancellationToken ct)
    {
        RequireOperator();

        var otp = (request.Otp ?? "").Trim();
        if (otp.Length != 6 || !otp.All(char.IsDigit))
        {
            throw new DomainException("Enter the 6-digit OTP.");
        }

        var state = await _sessions.TakeLoginStateAsync(request.ChallengeId, ct)
                    ?? throw new ForbiddenException("OTP challenge expired. Please try again.");

        if (string.IsNullOrWhiteSpace(state.OtpHash) || string.IsNullOrWhiteSpace(state.PendingMobile))
        {
            throw new ForbiddenException("Invalid OTP challenge.");
        }

        var expected = Convert.FromHexString(state.OtpHash);
        var actual = Convert.FromHexString(PortalAuthService.HashOtp(otp));
        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new ForbiddenException("Incorrect OTP. Please try again.");
        }

        var mobile = state.PendingMobile;
        var now = _clock.UtcNow;
        var citizen = await _portal.GetCitizenByMobileAsync(mobile, ct);
        if (citizen is null)
        {
            var last4 = mobile.Length >= 4 ? mobile[^4..] : "0000";
            citizen = new Citizen
            {
                Id = Guid.NewGuid(),
                // Not a real WSO2 identity -- this citizen was created by an operator bind, not a
                // self-service login. Prefixed so it can never collide with (or be mistaken for)
                // a genuine WSO2 subject or DigiLocker id.
                Wso2Subject = $"operator-bind:{Guid.NewGuid():N}",
                Username = mobile,
                Name = string.Empty,
                Mobile = mobile,
                MobileVerifiedAt = now,
                AadhaarMasked = $"XXXX-XXXX-{last4}",
                SevaId = $"SO{last4}KA",
                ProfileCompleted = false,
                CreatedByOperatorId = _current.OperatorCode,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _portal.InsertCitizenAsync(citizen, ct);
        }

        var sessionId = _current.SessionId!;
        var session = await _operatorSessions.GetAsync(sessionId, ct)
                      ?? throw new ForbiddenException("Operator session expired. Please log in again.");
        session.ActiveCitizenId = citizen.Id;
        var remainingTtl = session.ExpiresAt - now;
        await _operatorSessions.SaveAsync(session, remainingTtl > TimeSpan.Zero ? remainingTtl : TimeSpan.FromMinutes(1), ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            Action = "operator.citizen.bind",
            Resource = _current.OperatorCode,
            IpAddress = _current.IpAddress,
            At = now
        }, ct);

        return new BoundCitizenDto(citizen.SevaOneUserId, citizen.Name, citizen.Mobile);
    }

    public async Task<BoundCitizenDto?> GetBoundCitizenAsync(CancellationToken ct)
    {
        RequireOperator();

        if (_current.ActiveCitizenId is not { } citizenId)
        {
            return null;
        }

        var citizen = await _portal.GetCitizenByIdAsync(citizenId, ct);
        return citizen is null ? null : new BoundCitizenDto(citizen.SevaOneUserId, citizen.Name, citizen.Mobile);
    }

    public async Task UnbindAsync(CancellationToken ct)
    {
        RequireOperator();

        var sessionId = _current.SessionId!;
        var session = await _operatorSessions.GetAsync(sessionId, ct);
        if (session is null)
        {
            return;
        }

        session.ActiveCitizenId = null;
        var remainingTtl = session.ExpiresAt - _clock.UtcNow;
        await _operatorSessions.SaveAsync(session, remainingTtl > TimeSpan.Zero ? remainingTtl : TimeSpan.FromMinutes(1), ct);
    }

    private void RequireOperator()
    {
        if (!_current.IsAuthenticated || string.IsNullOrWhiteSpace(_current.SessionId))
        {
            throw new ForbiddenException("Not authenticated as an operator.");
        }
    }
}
