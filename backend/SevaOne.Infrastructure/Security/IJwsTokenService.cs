namespace SevaOne.Infrastructure.Security;

/// <summary>
/// Mints the SevaOne JWS handed to a department in the Apply URL. Distinct from WSO2's own
/// OIDC tokens: those stay inside this app; this one leaves it, embedded in a redirect the
/// citizen's browser follows to the department's own portal.
/// </summary>
public interface IJwsTokenService
{
    /// <summary>Signs a compact JWS carrying the handoff claims. Returns the token string plus its SHA-256 hex fingerprint — the fingerprint, not the raw token, is what's indexed in sevaone_token.token_hash.</summary>
    (string TokenValue, string TokenHash) Mint(JwsClaims claims);

    /// <summary>SHA-256 hex fingerprint of a token string — same algorithm Mint used, exposed so callers holding only the raw token (e.g. a department POST body) can look up its sevaone_token row.</summary>
    string ComputeHash(string tokenValue);

    /// <summary>
    /// Verifies the HMAC signature only — deliberately ignores the JWT's own `exp` claim.
    /// Expiry is a business rule that differs per call (30-minute Token Validate vs
    /// midnight-same-day Payment Status POST), so it's enforced by the caller against the
    /// sevaone_token row's own expire_at/midnight_valid_until columns, not by the JWT library.
    /// </summary>
    bool VerifySignature(string tokenValue);
}

public sealed record JwsClaims(
    long SevaOneUserId,
    string DepartmentId,
    string ServiceId,
    bool AssistedMode,
    string? OperatorId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpireAt);
