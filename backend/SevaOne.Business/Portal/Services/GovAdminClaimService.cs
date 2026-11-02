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
using Microsoft.Extensions.Logging;

namespace SevaOne.Business.Portal.Services;

/// <summary>
/// Gov-admin first-time-login claim -- mirrors OperatorClaimService almost verbatim. Superadmin
/// creates the gov_admin_master row upfront (username + tier/scope already assigned, no WSO2
/// account yet -- see V69__gov_admin_master_claim_flow.sql); this proves phone possession via
/// mobile OTP and is what actually creates the real WSO2 IS account and sets the password the
/// new admin will log in with from then on.
/// </summary>
public sealed class GovAdminClaimService : IGovAdminClaimService
{
    private static readonly Regex MobileFormat = new("^[0-9]{10,15}$");

    private readonly IPortalRepository _portal;
    private readonly ISevaSessionStore _sessions;
    private readonly ISmsOtpSender _smsOtp;
    private readonly IWso2GovAdminIdentityClient _wso2;
    private readonly IAuditRepository _audit;
    private readonly IClock _clock;
    private readonly ILogger<GovAdminClaimService> _log;

    public GovAdminClaimService(
        IPortalRepository portal,
        ISevaSessionStore sessions,
        ISmsOtpSender smsOtp,
        IWso2GovAdminIdentityClient wso2,
        IAuditRepository audit,
        IClock clock,
        ILogger<GovAdminClaimService> log)
    {
        _portal = portal;
        _sessions = sessions;
        _smsOtp = smsOtp;
        _wso2 = wso2;
        _audit = audit;
        _clock = clock;
        _log = log;
    }

    public async Task<GovAdminClaimStartResult> StartAsync(GovAdminClaimStartRequest request, CancellationToken ct)
    {
        var username = (request.Username ?? "").Trim();
        var mobile = (request.Mobile ?? "").Trim();
        if (username.Length < 3)
        {
            throw new DomainException("Enter a valid username.");
        }
        if (!MobileFormat.IsMatch(mobile))
        {
            throw new DomainException("Enter a valid mobile number.");
        }

        var password = request.Password ?? "";
        // Must match WSO2 IS's own password validation policy -- same check as
        // OperatorClaimService.StartAsync, see that method's doc comment for why.
        if (password.Length is < 8 or > 64
            || !password.Any(char.IsDigit)
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower))
        {
            throw new DomainException("Password must be 8-64 characters and include at least one uppercase letter, one lowercase letter, and one number.");
        }

        await EnsureClaimableAsync(username, ct);

        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var challengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        await _sessions.SaveLoginStateAsync(
            challengeId,
            new LoginState
            {
                PendingMobile = mobile,
                OtpHash = PortalAuthService.HashOtp(otp),
                PendingGovAdminUsername = username,
                PendingGovAdminPassword = password
            },
            TimeSpan.FromMinutes(10),
            ct);

        await _smsOtp.SendOtpAsync(mobile, otp, ct);
        _log.LogInformation("Gov-admin claim OTP for {Mobile}: {Otp}", mobile, otp);

        return new GovAdminClaimStartResult(challengeId);
    }

    public async Task<GovAdminClaimVerifyResult> VerifyAsync(GovAdminClaimVerifyRequest request, CancellationToken ct)
    {
        var otp = (request.Otp ?? "").Trim();
        if (otp.Length != 6 || !otp.All(char.IsDigit))
        {
            throw new DomainException("Enter the 6-digit OTP.");
        }

        var state = await _sessions.TakeLoginStateAsync(request.ChallengeId, ct)
                    ?? throw new ForbiddenException("OTP challenge expired. Please try again.");

        if (string.IsNullOrWhiteSpace(state.OtpHash)
            || string.IsNullOrWhiteSpace(state.PendingMobile)
            || string.IsNullOrWhiteSpace(state.PendingGovAdminUsername)
            || string.IsNullOrWhiteSpace(state.PendingGovAdminPassword))
        {
            throw new ForbiddenException("Invalid OTP challenge.");
        }

        var expected = Convert.FromHexString(state.OtpHash);
        var actual = Convert.FromHexString(PortalAuthService.HashOtp(otp));
        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new ForbiddenException("Incorrect OTP. Please try again.");
        }

        var username = state.PendingGovAdminUsername;
        // Defensive re-check: the row could have changed during the OTP wait (deactivated, or
        // claimed by a concurrent request) -- the UPDATE in sp_claim_gov_admin is still the
        // actual race-safe guard, this just gives a clearer error for the common case. Also
        // gives us the real display name Superadmin set at creation time, for the WSO2 SCIM2
        // account (rather than repeating the bare username).
        var creds = await EnsureClaimableAsync(username, ct);

        var (provisioned, errorDetail) = await _wso2.ProvisionAsync(username, creds.DisplayName, state.PendingMobile, state.PendingGovAdminPassword, ct);
        if (!provisioned)
        {
            throw new DomainException(errorDetail ?? "Could not create your account right now. Please try again.");
        }

        var now = _clock.UtcNow;
        var (success, message) = await _portal.ClaimGovAdminAsync(username, username, state.PendingMobile, now, ct);
        if (!success)
        {
            throw new ForbiddenException(message);
        }

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = null,
            Action = "gov_admin.claim",
            Resource = username,
            IpAddress = null,
            At = now
        }, ct);

        return new GovAdminClaimVerifyResult(username);
    }

    private async Task<GovAdminCredentials> EnsureClaimableAsync(string username, CancellationToken ct)
    {
        var creds = await _portal.GetGovAdminByUsernameAsync(username, ct);
        if (creds is null)
        {
            throw new DomainException("This username is not recognized.");
        }
        if (creds.Wso2Subject is not null)
        {
            throw new DomainException("This account has already been claimed. Please log in instead.");
        }
        if (!creds.IsActive)
        {
            throw new DomainException("This account is not active.");
        }
        return creds;
    }
}
