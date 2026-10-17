using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace SevaOne.Infrastructure.ExternalAuth;

public sealed class Wso2TokenIntrospectionCache : IWso2TokenIntrospectionCache
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Hard cap on how long a cached verdict can survive, regardless of the token's own remaining lifetime — bounds how stale a cached ACTIVE verdict can be after an out-of-band revoke that didn't go through SevaOne's own logout (which purges the cache directly instead of waiting this out).</summary>
    private static readonly TimeSpan MaxCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IConnectionMultiplexer _redis;
    private readonly HttpClient _http;
    private readonly Wso2CitizenIssuerOptions _options;
    private readonly ILogger<Wso2TokenIntrospectionCache> _log;

    public Wso2TokenIntrospectionCache(
        IConnectionMultiplexer redis,
        HttpClient http,
        IOptions<Wso2CitizenIssuerOptions> options,
        ILogger<Wso2TokenIntrospectionCache> log)
    {
        _redis = redis;
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<Wso2IntrospectionResult> ValidateAsync(string token, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(token);

        var cached = await db.StringGetAsync(key);
        if (!cached.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<Wso2IntrospectionResult>((string)cached!, Json)!;
        }

        var result = await IntrospectAsync(token, ct);

        var ttl = MaxCacheTtl;
        if (result.ExpiresAtUnix is { } exp)
        {
            var remaining = exp - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (remaining <= 0)
            {
                // Already expired at WSO2 — still worth a short cache so a burst of requests for
                // a dead token doesn't hammer WSO2, but not the full window.
                ttl = TimeSpan.FromSeconds(5);
            }
            else if (remaining < ttl.TotalSeconds)
            {
                ttl = TimeSpan.FromSeconds(remaining);
            }
        }

        await db.StringSetAsync(key, JsonSerializer.Serialize(result, Json), ttl);
        return result;
    }

    public async Task InvalidateAsync(string token, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(CacheKey(token));
    }

    private async Task<Wso2IntrospectionResult> IntrospectAsync(string token, CancellationToken ct)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token });
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ScimBaseUrl.TrimEnd('/') + "/oauth2/introspect")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ScimAdminUsername}:{_options.ScimAdminPassword}")));

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("WSO2 introspection failed ({Status}): {Body}", (int)response.StatusCode, body);
                return new Wso2IntrospectionResult(false, null, null);
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var active = root.TryGetProperty("active", out var a) && a.GetBoolean();
            var username = root.TryGetProperty("username", out var u) ? u.GetString() : null;
            long? exp = root.TryGetProperty("exp", out var e) && e.ValueKind == JsonValueKind.Number ? e.GetInt64() : null;
            var scope = root.TryGetProperty("scope", out var s) ? s.GetString() : null;
            return new Wso2IntrospectionResult(active, username, exp, scope);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "WSO2 introspection call failed");
            return new Wso2IntrospectionResult(false, null, null);
        }
    }

    private static string CacheKey(string token) =>
        "wso2:introspect:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
