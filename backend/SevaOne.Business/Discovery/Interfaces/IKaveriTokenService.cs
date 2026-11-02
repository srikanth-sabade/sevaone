using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

public sealed record KaveriTokenResult(string? AccessToken, string? Error, int? HttpStatus = null);

/// <summary>Holds the sign-in to Kaveri for the whole platform: one technical login, a short-lived access token and a
/// single-use refresh token, shared by every running copy of the backend.</summary>
public interface IKaveriTokenService
{
    /// <summary>The Kaveri settings and password saved in the admin console, or null (with a reason) when incomplete.</summary>
    Task<(KaveriSettings? Settings, string? Password, string? Problem)> LoadProviderAsync(CancellationToken ct);

    /// <summary>A usable access token: the cached one, else a refresh, else a fresh sign-in. forceNew skips the cached token
    /// (after Kaveri rejected it).</summary>
    Task<KaveriTokenResult> GetAccessTokenAsync(bool forceNew, CancellationToken ct);

    /// <summary>Admin: signs in for real (ignoring any cached token) and says whether it worked. Never returns the token.</summary>
    Task<TokenProviderTestResult> TestLoginAsync(CancellationToken ct);
}
