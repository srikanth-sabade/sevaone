namespace SevaOne.Infrastructure.ExternalAuth;

public sealed record Wso2IntrospectionResult(bool Active, string? Username, long? ExpiresAtUnix, string? Scope = null);

/// <summary>
/// Redis-cached WSO2 token introspection. The citizen auth middleware needs to know "is this
/// token still good" on every single request — calling WSO2's /oauth2/introspect live each time
/// would put an external network dependency on the hot path of every citizen request. Instead,
/// the first check for a given token calls WSO2 once and caches the answer in Redis (shared
/// across all backend instances) for a short, bounded window; every check within that window is
/// a local Redis lookup. <see cref="InvalidateAsync"/> lets logout purge a token's cached
/// "active" verdict immediately, instead of waiting out the cache window.
/// </summary>
public interface IWso2TokenIntrospectionCache
{
    Task<Wso2IntrospectionResult> ValidateAsync(string token, CancellationToken ct);

    Task InvalidateAsync(string token, CancellationToken ct);
}
