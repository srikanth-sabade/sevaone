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
/// Self-claim flow for operators listed in portal.operator_whitelist (a department-supplied
/// roster of real user_ids, no credentials) -- proves phone possession via mobile OTP, mirroring
/// how citizens actually prove identity (mobile OTP + DigiLocker). Unlike citizens, though,
/// operators' resulting credential lives entirely in WSO2 IS (IWso2OperatorIdentityClient) --
/// see backend/docs/WSO2_IS_ROADMAP.md section 2.3. operator_master keeps no password for a
/// newly-claimed operator, only the wso2_subject link.
/// </summary>
public sealed class OperatorClaimService : IOperatorClaimService
{
    private static readonly Regex MobileFormat = new("^[0-9]{10,15}$");

    private readonly IPortalRepository _portal;
    private readonly ISevaSessionStore _sessions;
    private readonly ISmsOtpSender _smsOtp;
    private readonly IWso2OperatorIdentityClient _wso2;
    private readonly IAuditRepository _audit;
    private readonly IClock _clock;
    private readonly ILogger<OperatorClaimService> _log;

    public OperatorClaimService(
        IPortalRepository portal,
        ISevaSessionStore sessions,
        ISmsOtpSender smsOtp,
        IWso2OperatorIdentityClient wso2,
        IAuditRepository audit,
        IClock clock,
        ILogger<OperatorClaimService> log)
    {
        _portal = portal;
        _sessions = sessions;
        _smsOtp = smsOtp;
        _wso2 = wso2;
        _audit = audit;
        _clock = clock;
        _log = log;
    }

    public async Task<OperatorClaimStartResult> StartAsync(OperatorClaimStartRequest request, CancellationToken ct)
    {
        var mobile = (request.Mobile ?? "").Trim();
        if (!MobileFormat.IsMatch(mobile))
        {
            throw new DomainException("Enter a valid mobile number.");
        }

        var password = request.Password ?? "";
        // Must match WSO2 IS's own password validation policy exactly (queried live via
        // /api/server/v1/validation-rules/password against this deployment's WSO2 IS: 8-64
        // chars, >=1 digit, >=1 uppercase, >=1 lowercase, special characters optional) -- a
        // weaker check here just means the claim fails later at WSO2 account creation with a
        // generic "could not create your account" instead of a clear reason. Confirmed
        // empirically: a real claim attempt failed this way before this check existed.
        if (password.Length is < 8 or > 64
            || !password.Any(char.IsDigit)
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower))
        {
            throw new DomainException("Password must be 8-64 characters and include at least one uppercase letter, one lowercase letter, and one number.");
        }

        await EnsureClaimableAsync(request.UserId, ct);

        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var challengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        await _sessions.SaveLoginStateAsync(
            challengeId,
            new LoginState
            {
                PendingMobile = mobile,
                OtpHash = PortalAuthService.HashOtp(otp),
                PendingOperatorUserId = request.UserId,
                PendingOperatorPassword = password
            },
            TimeSpan.FromMinutes(10),
            ct);

        await _smsOtp.SendOtpAsync(mobile, otp, ct);
        _log.LogInformation("Operator claim OTP for {Mobile}: {Otp}", mobile, otp);

        return new OperatorClaimStartResult(challengeId);
    }

    public async Task<OperatorClaimVerifyResult> VerifyAsync(OperatorClaimVerifyRequest request, CancellationToken ct)
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
            || state.PendingOperatorUserId is not { } userId
            || string.IsNullOrWhiteSpace(state.PendingOperatorPassword))
        {
            throw new ForbiddenException("Invalid OTP challenge.");
        }

        var expected = Convert.FromHexString(state.OtpHash);
        var actual = Convert.FromHexString(PortalAuthService.HashOtp(otp));
        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new ForbiddenException("Incorrect OTP. Please try again.");
        }

        // Defensive re-check: the whitelist/claim state could have changed during the OTP wait
        // (another request claimed it, or it was somehow removed from the roster). The insert
        // itself is still the actual race-safe guard (unique_violation) -- this just gives a
        // clearer error than a generic failure for the common case.
        await EnsureClaimableAsync(userId, ct);

        var operatorId = userId.ToString();
        var wso2Subject = $"sevaone-operator-{userId}";
        // No real name exists anywhere in the whitelist roster (see V54's comment on
        // operator_whitelist) -- WSO2's SCIM2 schema wants a name regardless, so this is a
        // plainly-generic placeholder, not a fabricated claim about who this person is.
        var placeholderName = $"Operator {userId}";

        var (provisioned, errorDetail) = await _wso2.ProvisionAsync(wso2Subject, placeholderName, state.PendingMobile, state.PendingOperatorPassword, ct);
        if (!provisioned)
        {
            // Not soft-fail like citizen provisioning -- operators have nowhere else for a
            // credential to live, so a failed WSO2 account creation must fail the whole claim.
            // errorDetail carries WSO2's own reason when it's a safe-to-show validation failure
            // (see Wso2OperatorIdentityClient.TryExtractScimValidationDetail) -- falls back to
            // a generic message otherwise (WSO2 unreachable, etc.).
            throw new DomainException(errorDetail ?? "Could not create your account right now. Please try again.");
        }

        var now = _clock.UtcNow;
        var (success, message, id) = await _portal.ClaimOperatorAsync(
            Guid.NewGuid(), operatorId, state.PendingMobile, now, wso2Subject, now, ct);

        if (!success || id is null)
        {
            throw new ForbiddenException(message);
        }

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = null,
            Action = "operator.claim",
            Resource = operatorId,
            IpAddress = null,
            At = now
        }, ct);

        return new OperatorClaimVerifyResult(operatorId);
    }

    private async Task EnsureClaimableAsync(long userId, CancellationToken ct)
    {
        var whitelisted = await _portal.GetOperatorWhitelistEntryAsync(userId, ct);
        if (whitelisted is null)
        {
            throw new DomainException("This operator ID is not recognized.");
        }

        var existing = await _portal.GetOperatorByCodeAsync(userId.ToString(), ct);
        if (existing is not null)
        {
            throw new DomainException("This operator ID has already been claimed. Please log in instead.");
        }
    }
}
