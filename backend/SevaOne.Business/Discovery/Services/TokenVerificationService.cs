using System.Security.Cryptography;
using System.Text;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// The token check for the Seva Sindhu V3 hand-off. The contract says the department must verify the token but not how, so this
/// is how: the department sends the token and the application reference it received, and we say whether it is the token we issued
/// for that application, for that department, and not expired, and give the few user details the service is set to share.
/// A repeat check inside the token's life still works and says so, so a page refresh does not lock the citizen out.
/// </summary>
public sealed class TokenVerificationService : ITokenVerificationService
{
    private readonly ITokenIntegrationRepository _integrations;
    private readonly IApplyRedirectProtector _protector;
    private readonly IClock _clock;

    public TokenVerificationService(ITokenIntegrationRepository integrations, IApplyRedirectProtector protector, IClock clock)
    {
        _integrations = integrations;
        _protector = protector;
        _clock = clock;
    }

    public async Task<TokenVerifyResult> VerifyAsync(TokenVerifyRequest request, TokenIntegrationCaller caller, CancellationToken ct)
    {
        var reference = request.EdcsApplicationRefNo?.Trim();
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(reference) || reference.Length > 32 || string.IsNullOrEmpty(token))
            return new TokenVerifyResult(false, "missing_fields");

        var info = await _integrations.GetVerifyInfoAsync(reference, ct);
        if (info is null) return new TokenVerifyResult(false, "unknown_application");
        if (info.DepartmentId != caller.DepartmentId) return new TokenVerifyResult(false, "wrong_department", Sandbox: info.IsSandbox);

        var plain = _protector.TryUnprotect(token);
        var matches = plain is not null && info.TokenHash is not null
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sha256Hex(plain)), Encoding.ASCII.GetBytes(info.TokenHash.ToLowerInvariant()));
        if (!matches) return new TokenVerifyResult(false, "token_mismatch", Sandbox: info.IsSandbox);
        if (info.TokenRevoked) return new TokenVerifyResult(false, "token_revoked", Sandbox: info.IsSandbox);
        if (info.ExpiresAt <= _clock.UtcNow) return new TokenVerifyResult(false, "token_expired", Sandbox: info.IsSandbox);

        var already = await _integrations.MarkTokenVerifiedAsync(info.AttemptId, ct);
        return new TokenVerifyResult(
            Valid: true,
            UserId: info.SevaOneUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            EdcsApplicationRefNo: info.EdcsApplicationRefNo,
            ServiceId: info.ServiceValue,
            AssistedMode: info.AssistedMode,
            Name: info.CitizenName,
            Mobile: info.SendMobile ? info.CitizenMobile : null,
            DigilockerId: info.SendDigilockerId ? info.CitizenDigilockerId : null,
            ExpiresAt: info.ExpiresAt,
            AlreadyVerified: already,
            Sandbox: info.IsSandbox);
    }

    private static string Sha256Hex(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
