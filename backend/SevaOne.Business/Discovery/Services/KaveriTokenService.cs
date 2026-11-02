using System.Text;
using System.Text.Json;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// Sign-in to Kaveri 2.0. The access token lasts 300 seconds and the refresh token 1800, and a refresh token works only
/// once, so renewal is done by one caller at a time (a short shared lock) and the new pair is stored before anyone else
/// looks. Order of preference: the stored access token, then a refresh, then a fresh sign-in with the technical login.
/// Tokens are encrypted before they are stored.
/// </summary>
public sealed class KaveriTokenService : IKaveriTokenService
{
    public const string TypeKey = "kaveri_sso";
    private const string AccessKey = "kaveri:access";
    private const string RefreshKey = "kaveri:refresh";
    private const string LockKey = "kaveri:renew";
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultAccessLife = TimeSpan.FromSeconds(300);
    private static readonly TimeSpan DefaultRefreshLife = TimeSpan.FromSeconds(1800);
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly ITokenIntegrationRepository _integrations;
    private readonly ITokenCache _cache;
    private readonly ITokenStatusHttpClient _http;
    private readonly ITokenStatusSecretProtector _secrets;
    private readonly SevaOne.Infrastructure.Security.IClock _clock;

    public KaveriTokenService(
        ITokenIntegrationRepository integrations, ITokenCache cache, ITokenStatusHttpClient http,
        ITokenStatusSecretProtector secrets, SevaOne.Infrastructure.Security.IClock clock)
    {
        _integrations = integrations;
        _cache = cache;
        _http = http;
        _secrets = secrets;
        _clock = clock;
    }

    public async Task<(KaveriSettings? Settings, string? Password, string? Problem)> LoadProviderAsync(CancellationToken ct)
    {
        var row = await _integrations.GetProviderAsync(TypeKey, ct);
        if (row is null) return (null, null, "The Kaveri settings have not been saved yet.");
        var settings = ReadSettings(row.SettingsJson);
        if (string.IsNullOrWhiteSpace(settings.LoginUrl) || string.IsNullOrWhiteSpace(settings.SsoUrl) || string.IsNullOrWhiteSpace(settings.Username))
            return (null, null, "The Kaveri settings are incomplete (sign-in address, single sign-on address and username are needed).");
        if (string.IsNullOrEmpty(row.SecretCiphertext)) return (null, null, "The Kaveri password has not been saved yet.");
        var password = _secrets.TryUnprotect(row.SecretCiphertext);
        return password is null ? (null, null, "The saved Kaveri password could not be read. Enter it again.") : (settings, password, null);
    }

    public static KaveriSettings ReadSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new KaveriSettings();
        try { return JsonSerializer.Deserialize<KaveriSettings>(json, Web) ?? new KaveriSettings(); }
        catch (JsonException) { return new KaveriSettings(); }
    }

    public async Task<KaveriTokenResult> GetAccessTokenAsync(bool forceNew, CancellationToken ct)
    {
        if (!forceNew && await ReadAsync(AccessKey, ct) is { } cached) return new KaveriTokenResult(cached, null);

        var (settings, password, problem) = await LoadProviderAsync(ct);
        if (settings is null) return new KaveriTokenResult(null, problem);

        // Only one caller renews at a time; the others wait briefly for the result instead of spending the refresh token twice.
        await using var held = await _cache.TryLockAsync(LockKey, TimeSpan.FromSeconds(20), ct);
        if (held is null)
        {
            for (var i = 0; i < 12; i++)
            {
                await Task.Delay(500, ct);
                if (await ReadAsync(AccessKey, ct) is { } shared) return new KaveriTokenResult(shared, null);
            }
            return new KaveriTokenResult(null, "Kaveri sign-in is busy. Please try again.");
        }

        if (!forceNew && await ReadAsync(AccessKey, ct) is { } renewedMeanwhile) return new KaveriTokenResult(renewedMeanwhile, null);

        if (!string.IsNullOrWhiteSpace(settings.RefreshUrl) && await ReadAsync(RefreshKey, ct) is { } refreshToken)
        {
            var refreshed = await RefreshAsync(settings, refreshToken, ct);
            if (refreshed.AccessToken is not null) return refreshed;
            await _cache.RemoveAsync(RefreshKey, ct); // spent or rejected: fall back to a fresh sign-in
        }

        return await LoginAsync(settings, password!, ct);
    }

    public async Task<TokenProviderTestResult> TestLoginAsync(CancellationToken ct)
    {
        var (settings, password, problem) = await LoadProviderAsync(ct);
        if (settings is null) return new TokenProviderTestResult(false, problem!);

        await using var held = await _cache.TryLockAsync(LockKey, TimeSpan.FromSeconds(20), ct);
        if (held is null) return new TokenProviderTestResult(false, "Another sign-in is in progress. Try again in a moment.");
        var result = await LoginAsync(settings, password!, ct);
        return result.AccessToken is not null
            ? new TokenProviderTestResult(true, "Signed in to Kaveri.")
            : new TokenProviderTestResult(false, result.Error ?? "Sign-in failed.");
    }

    private async Task<KaveriTokenResult> LoginAsync(KaveriSettings settings, string password, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new Dictionary<string, string> { ["username"] = settings.Username, ["password"] = password });
        var reply = await _http.PostJsonAsync(settings.LoginUrl, body, new TokenStatusAuth("none", null, null, null), ct);
        if (reply.Error is not null) return new KaveriTokenResult(null, reply.Error);
        if (reply.HttpStatus is not (>= 200 and < 300)) return new KaveriTokenResult(null, DescribeFailure("Kaveri sign-in", reply), reply.HttpStatus);

        try
        {
            using var doc = JsonDocument.Parse(reply.Body ?? "");
            var data = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;
            var jwt = Str(data, "JwtToken");
            var refresh = Str(data, "RefreshToken");
            if (string.IsNullOrEmpty(jwt)) return new KaveriTokenResult(null, "Kaveri's sign-in reply had no access token.");
            await StoreAsync(jwt, ExpiryOf(jwt) ?? DefaultAccessLife, refresh, DefaultRefreshLife, ct);
            return new KaveriTokenResult(jwt, null);
        }
        catch (JsonException)
        {
            return new KaveriTokenResult(null, "Kaveri's sign-in reply is not JSON.");
        }
    }

    private async Task<KaveriTokenResult> RefreshAsync(KaveriSettings settings, string refreshToken, CancellationToken ct)
    {
        var reply = await _http.PostJsonAsync(settings.RefreshUrl, JsonSerializer.Serialize(new Dictionary<string, string> { ["refresh_token"] = refreshToken }),
            new TokenStatusAuth("none", null, null, null), ct);
        if (reply.Error is not null || reply.HttpStatus is not (>= 200 and < 300)) return new KaveriTokenResult(null, reply.Error ?? "Refresh refused.", reply.HttpStatus);

        try
        {
            using var doc = JsonDocument.Parse(reply.Body ?? "");
            var root = doc.RootElement;
            var access = Str(root, "access_token");
            if (string.IsNullOrEmpty(access)) return new KaveriTokenResult(null, "Kaveri's refresh reply had no access token.");
            var accessLife = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var seconds) && seconds > 0 ? TimeSpan.FromSeconds(seconds) : (ExpiryOf(access) ?? DefaultAccessLife);
            var refreshLife = root.TryGetProperty("refresh_expires_in", out var r) && r.TryGetInt32(out var rs) && rs > 0 ? TimeSpan.FromSeconds(rs) : DefaultRefreshLife;
            await StoreAsync(access, accessLife, Str(root, "refresh_token"), refreshLife, ct);
            return new KaveriTokenResult(access, null);
        }
        catch (JsonException)
        {
            return new KaveriTokenResult(null, "Kaveri's refresh reply is not JSON.");
        }
    }

    private async Task StoreAsync(string access, TimeSpan accessLife, string? refresh, TimeSpan refreshLife, CancellationToken ct)
    {
        var accessKeep = accessLife - Margin;
        if (accessKeep > TimeSpan.Zero) await _cache.SetAsync(AccessKey, _secrets.Protect(access), accessKeep, ct);
        if (!string.IsNullOrEmpty(refresh)) await _cache.SetAsync(RefreshKey, _secrets.Protect(refresh), refreshLife - TimeSpan.FromSeconds(60), ct);
        else await _cache.RemoveAsync(RefreshKey, ct);
    }

    private async Task<string?> ReadAsync(string key, CancellationToken ct)
    {
        var stored = await _cache.GetAsync(key, ct);
        return stored is null ? null : _secrets.TryUnprotect(stored);
    }

    private static string? Str(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString();
        return null;
    }

    /// <summary>How long the token still has to live, from its own "exp" claim (the sign-in reply does not say). Null when unreadable.</summary>
    private TimeSpan? ExpiryOf(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return null;
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            if (doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var unix))
            {
                var life = DateTimeOffset.FromUnixTimeSeconds(unix) - _clock.UtcNow;
                return life > TimeSpan.Zero ? life : null;
            }
        }
        catch (Exception e) when (e is FormatException or JsonException) { }
        return null;
    }

    public static string DescribeFailure(string what, TokenStatusHttpResult reply)
    {
        var detail = "";
        try
        {
            using var doc = JsonDocument.Parse(reply.Body ?? "");
            if (doc.RootElement.TryGetProperty("errorMessage", out var m) && m.ValueKind == JsonValueKind.String) detail = $" ({m.GetString()})";
        }
        catch (JsonException) { }
        return $"{what} was refused with HTTP {reply.HttpStatus}{detail}.";
    }
}
