namespace SevaOne.Infrastructure.ExternalAuth;

public sealed record Wso2CitizenTokens(string AccessToken, string? RefreshToken, int ExpiresIn);

/// <summary>
/// Gives a citizen who already proved their identity via DigiLocker/mobile-OTP a REAL WSO2 IS
/// identity and token pair, so a future integration (another department, a mobile app) can
/// trust WSO2 directly — introspection, refresh — instead of only SevaOne's own session cookie.
/// Find-or-create against SCIM2, then a JWT-bearer (RFC 7523) token exchange; SevaOne's own
/// session/login flow is unaffected either way — every call here is soft-fail (see
/// <see cref="Wso2CitizenIdentityClient"/>), never blocks a citizen's login.
/// </summary>
public interface IWso2CitizenIdentityClient
{
    /// <summary>
    /// Ensures a WSO2 IS local user exists for this citizen (keyed on the stable, non-PII
    /// "sevaone-{SevaOneUserId}" username) and exchanges a signed assertion for a real WSO2
    /// access/refresh token pair. Returns null on any failure — callers fall back to their
    /// existing session token, they never surface this as a login error.
    /// </summary>
    Task<Wso2CitizenTokens?> ProvisionAndExchangeAsync(
        long sevaOneUserId, string name, string? email, string? mobile, CancellationToken ct);

    /// <summary>
    /// Exchanges a refresh token for a fresh access/refresh token pair (grant_type=refresh_token).
    /// Returns null on any failure (expired/revoked refresh token, WSO2 unreachable) — the
    /// caller's job, not this method's, to decide what "no fresh token" means for the citizen
    /// (typically: clear cookies, force a fresh login).
    /// </summary>
    Task<Wso2CitizenTokens?> RefreshAsync(string refreshToken, CancellationToken ct);

    /// <summary>
    /// Revokes a token previously issued by THIS exchange. Must use this client's own
    /// credentials — WSO2 rejects a revoke from a different OAuth client than the one the
    /// token was issued to. Soft-fail, best-effort, same as everything else here.
    /// </summary>
    Task RevokeAsync(string token, CancellationToken ct);
}
