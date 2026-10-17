using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// SCIM2 find-or-create against WSO2 IS's local userstore, then an RFC 7523 JWT-bearer token
/// exchange against the Trusted Token Issuer registered for SevaOne (see
/// docs/WSO2_IS_APIM_SETUP.md). Every public method here is soft-fail: WSO2 being unreachable,
/// misconfigured, or rejecting a call must never block a citizen's SevaOne login — it only means
/// this citizen doesn't (yet) have a portable WSO2 identity.
/// </summary>
public sealed class Wso2CitizenIdentityClient : IWso2CitizenIdentityClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly Wso2CitizenIssuerOptions _issuerOptions;
    private readonly ILogger<Wso2CitizenIdentityClient> _log;

    public Wso2CitizenIdentityClient(
        HttpClient http,
        IOptions<Wso2CitizenIssuerOptions> issuerOptions,
        ILogger<Wso2CitizenIdentityClient> log)
    {
        _http = http;
        _issuerOptions = issuerOptions.Value;
        _log = log;
    }

    public async Task<Wso2CitizenTokens?> ProvisionAndExchangeAsync(
        long sevaOneUserId, string name, string? email, string? mobile, CancellationToken ct)
    {
        var scimUsername = $"sevaone-{sevaOneUserId}";
        try
        {
            var provisioned = await FindOrCreateScimUserAsync(scimUsername, name, email, mobile, ct);
            if (!provisioned)
            {
                return null;
            }

            var assertion = MintAssertion(scimUsername);
            return await ExchangeAsync(assertion, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "WSO2 citizen identity provisioning/token-exchange failed for {ScimUsername} — session continues on SevaOne's own token only",
                scimUsername);
            return null;
        }
    }

    private async Task<bool> FindOrCreateScimUserAsync(string scimUsername, string name, string? email, string? mobile, CancellationToken ct)
    {
        if (await ScimUserExistsAsync(scimUsername, ct))
        {
            return true;
        }

        var (givenName, familyName) = SplitName(name);
        var attributes = new Dictionary<string, object?>
        {
            ["schemas"] = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            ["userName"] = scimUsername,
            // Never used to authenticate — the citizen only ever reaches WSO2 via the JWT-bearer
            // exchange below, never a WSO2 password login. Required by SCIM2 user creation only.
            // 20 bytes -> 28-char Base64, under WSO2's default password regex cap of 30 chars
            // (24 bytes produces 32 chars and is silently rejected as "Credential is not valid").
            ["password"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(20)),
            ["name"] = new { formatted = name, givenName, familyName },
            // SevaOne is Karnataka/India-only -- no per-citizen country field exists to source
            // this from, so it's a fixed value, not a guess. Fills WSO2 Console's Address tab
            // instead of leaving it blank.
            ["addresses"] = new[] { new { type = "home", country = "India" } }
        };

        var emails = new List<object>();
        if (!string.IsNullOrWhiteSpace(email))
        {
            emails.Add(new { value = email, primary = true });
        }
        if (emails.Count > 0)
        {
            attributes["emails"] = emails;
        }

        if (!string.IsNullOrWhiteSpace(mobile))
        {
            attributes["phoneNumbers"] = new[] { new { value = mobile, type = "mobile" } };
        }

        using var request = ScimRequest(HttpMethod.Post, "/scim2/Users");
        request.Content = new StringContent(JsonSerializer.Serialize(attributes, Json), Encoding.UTF8, "application/scim+json");
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        // Lost a create race against another request for the same citizen — fine, the user exists now.
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return true;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        _log.LogWarning("WSO2 SCIM2 user create failed for {ScimUsername} ({Status}): {Body}", scimUsername, (int)response.StatusCode, body);
        return false;
    }

    /// <summary>
    /// SevaOne only ever collects one free-text "full name" field, but WSO2 IS's default SCIM2
    /// user schema treats givenName/familyName as separate attributes -- leaving familyName out
    /// entirely (as the previous code did) shows up in the IS Console as a blank, flagged-required
    /// "Last Name" field. Splits on the last space; a single-word name (common for many citizens)
    /// gets used for both, rather than leaving familyName empty.
    /// </summary>
    private static (string GivenName, string FamilyName) SplitName(string fullName)
    {
        var trimmed = fullName.Trim();
        var lastSpace = trimmed.LastIndexOf(' ');
        return lastSpace <= 0 ? (trimmed, trimmed) : (trimmed[..lastSpace].Trim(), trimmed[(lastSpace + 1)..].Trim());
    }

    private async Task<bool> ScimUserExistsAsync(string scimUsername, CancellationToken ct)
    {
        var filter = Uri.EscapeDataString($"userName eq \"{scimUsername}\"");
        using var request = ScimRequest(HttpMethod.Get, $"/scim2/Users?filter={filter}");
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("totalResults", out var total) && total.GetInt32() > 0;
    }

    private HttpRequestMessage ScimRequest(HttpMethod method, string pathAndQuery)
    {
        var request = new HttpRequestMessage(method, _issuerOptions.ScimBaseUrl.TrimEnd('/') + pathAndQuery);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_issuerOptions.ScimAdminUsername}:{_issuerOptions.ScimAdminPassword}")));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/scim+json"));
        return request;
    }

    private string MintAssertion(string scimUsername)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_issuerOptions.SigningKeyPem);
        var key = new RsaSecurityKey(rsa)
        {
            KeyId = null,
            // Microsoft.IdentityModel caches SignatureProviders per key process-wide by default
            // (CryptoProviderFactory.Default). Without this, the provider built on THIS call's
            // RSA instance survives in that cache after `rsa` is disposed below, and the next
            // call reuses it -> ObjectDisposedException on RSABCrypt deep inside WriteToken.
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: _issuerOptions.Issuer,
            audience: _issuerOptions.Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, scimUsername)],
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private async Task<Wso2CitizenTokens?> ExchangeAsync(string assertion, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = assertion
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _issuerOptions.TokenEndpoint) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_issuerOptions.ClientId}:{_issuerOptions.ClientSecret}")));

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _log.LogWarning("WSO2 JWT-bearer token exchange failed ({Status}): {Body}", (int)response.StatusCode, body);
            return null;
        }

        return ParseTokenResponse(body);
    }

    public async Task<Wso2CitizenTokens?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, _issuerOptions.TokenEndpoint) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_issuerOptions.ClientId}:{_issuerOptions.ClientSecret}")));

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("WSO2 refresh-token exchange failed ({Status}): {Body}", (int)response.StatusCode, body);
                return null;
            }

            return ParseTokenResponse(body);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "WSO2 refresh-token exchange failed");
            return null;
        }
    }

    private static Wso2CitizenTokens? ParseTokenResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;
        return new Wso2CitizenTokens(accessToken, refreshToken, expiresIn);
    }

    public async Task RevokeAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_issuerOptions.ClientId))
        {
            // Not a real WSO2 token to begin with (provisioning never ran, or is unconfigured) —
            // nothing to revoke.
            return;
        }

        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token });
            using var request = new HttpRequestMessage(HttpMethod.Post, _issuerOptions.ScimBaseUrl.TrimEnd('/') + "/oauth2/revoke")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_issuerOptions.ClientId}:{_issuerOptions.ClientSecret}")));
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _log.LogWarning("WSO2 citizen token revoke failed ({Status}): {Body}", (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "WSO2 citizen token revoke failed");
        }
    }
}
