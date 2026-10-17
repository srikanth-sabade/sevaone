using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// OAuth2 password-grant login + SCIM2 group check for the government admin panel against
/// WSO2 IS -- near-verbatim mirror of Wso2OperatorIdentityClient, minus provisioning (see
/// IWso2GovAdminIdentityClient's doc comment). Shares the SCIM base URL and admin credentials
/// with Wso2CitizenIssuerOptions (same WSO2 IS instance).
///
/// RBAC note: same empirically confirmed WSO2 IS 7.3.0 limitation as the operator client --
/// SCIM2 group membership isn't surfaced in the password grant's id_token or
/// /oauth2/introspect. The reliable path is a plain SCIM2 GET on the user (admin credentials)
/// after a successful password-grant login, checking the returned `groups` array.
/// </summary>
public sealed class Wso2GovAdminIdentityClient : IWso2GovAdminIdentityClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly Wso2GovAdminIssuerOptions _govAdminOptions;
    private readonly Wso2CitizenIssuerOptions _scimOptions;
    private readonly ILogger<Wso2GovAdminIdentityClient> _log;

    public Wso2GovAdminIdentityClient(
        HttpClient http,
        IOptions<Wso2GovAdminIssuerOptions> govAdminOptions,
        IOptions<Wso2CitizenIssuerOptions> scimOptions,
        ILogger<Wso2GovAdminIdentityClient> log)
    {
        _http = http;
        _govAdminOptions = govAdminOptions.Value;
        _scimOptions = scimOptions.Value;
        _log = log;
    }

    public async Task<(bool Success, string? ErrorDetail)> ProvisionAsync(string scimUsername, string name, string mobile, string password, CancellationToken ct)
    {
        try
        {
            var (givenName, familyName) = SplitName(name);
            var attributes = new Dictionary<string, object?>
            {
                ["schemas"] = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                ["userName"] = scimUsername,
                ["password"] = password,
                ["name"] = new { formatted = name, givenName, familyName },
                ["phoneNumbers"] = new[] { new { value = mobile, type = "mobile" } }
            };

            using var request = ScimRequest(HttpMethod.Post, "/scim2/Users");
            request.Content = new StringContent(JsonSerializer.Serialize(attributes, Json), Encoding.UTF8, "application/scim+json");
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _log.LogError("WSO2 gov-admin SCIM2 user create failed for {ScimUsername} ({Status}): {Body}", scimUsername, (int)response.StatusCode, body);
                return (false, TryExtractScimValidationDetail(body));
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var userId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _log.LogError("WSO2 gov-admin SCIM2 user create for {ScimUsername} returned no id", scimUsername);
                return (false, null);
            }

            var groupId = await FindGroupIdAsync(ct);
            if (groupId is null)
            {
                _log.LogError("WSO2 gov-admin RBAC group {GroupName} does not exist -- run Docker-deploy/wso2/provision.sh", _govAdminOptions.GroupName);
                return (false, null);
            }

            return (await AddUserToGroupAsync(groupId, userId, scimUsername, ct), null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "WSO2 gov-admin provisioning failed for {ScimUsername}", scimUsername);
            return (false, null);
        }
    }

    /// <summary>Only "invalidValue" SCIM2 errors are treated as safe-to-show validation detail
    /// (e.g. password policy) -- other scimTypes (or a body that isn't SCIM2 error JSON at all)
    /// return null, letting the caller fall back to its own generic message.</summary>
    private static string? TryExtractScimValidationDetail(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var scimType = doc.RootElement.TryGetProperty("scimType", out var t) ? t.GetString() : null;
            var detail = doc.RootElement.TryGetProperty("detail", out var d) ? d.GetString() : null;
            return scimType == "invalidValue" && !string.IsNullOrWhiteSpace(detail) ? detail : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<string?> FindGroupIdAsync(CancellationToken ct)
    {
        var filter = Uri.EscapeDataString($"displayName eq {_govAdminOptions.GroupName}");
        using var request = ScimRequest(HttpMethod.Get, $"/scim2/Groups?filter={filter}");
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("Resources", out var resources) || resources.GetArrayLength() == 0)
        {
            return null;
        }

        return resources[0].TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
    }

    private async Task<bool> AddUserToGroupAsync(string groupId, string userId, string scimUsername, CancellationToken ct)
    {
        using var request = ScimRequest(HttpMethod.Patch, $"/scim2/Groups/{groupId}");
        // Dictionary, not an anonymous type: Json uses JsonSerializerDefaults.Web, whose
        // camelCase naming policy would rewrite "Operations" to "operations" on a POCO/anonymous
        // type's property names -- SCIM2's PatchOp schema requires the exact capitalized
        // "Operations". Same reason Wso2OperatorIdentityClient's SCIM2 bodies are all
        // Dictionary<string, object?>, not anonymous types.
        var patch = new Dictionary<string, object?>
        {
            ["schemas"] = new[] { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
            ["Operations"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["op"] = "add",
                    ["path"] = "members",
                    ["value"] = new[] { new Dictionary<string, object?> { ["value"] = userId, ["display"] = scimUsername } }
                }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(patch, Json), Encoding.UTF8, "application/scim+json");
        using var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("WSO2 gov-admin group-membership PATCH failed for {ScimUsername} ({Status}): {Body}", scimUsername, (int)response.StatusCode, body);
        return false;
    }

    // Same rationale as Wso2CitizenIdentityClient.SplitName -- WSO2's default SCIM2 schema wants
    // givenName/familyName separately; a single-word name is used for both rather than leaving
    // familyName blank.
    private static (string GivenName, string FamilyName) SplitName(string fullName)
    {
        var trimmed = fullName.Trim();
        var lastSpace = trimmed.LastIndexOf(' ');
        return lastSpace <= 0 ? (trimmed, trimmed) : (trimmed[..lastSpace].Trim(), trimmed[(lastSpace + 1)..].Trim());
    }

    public async Task<Wso2GovAdminLoginResult> LoginAsync(string scimUsername, string password, CancellationToken ct)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = scimUsername,
                ["password"] = password,
                ["scope"] = "openid"
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, _govAdminOptions.TokenEndpoint) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_govAdminOptions.ClientId}:{_govAdminOptions.ClientSecret}")));

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Wrong password, or account locked/disabled -- not logged as an error, this is
                // an expected outcome of a login attempt, not a system fault.
                return new Wso2GovAdminLoginResult(false, false);
            }

            var inGroup = await IsInGovAdminGroupAsync(scimUsername, ct);
            return new Wso2GovAdminLoginResult(true, inGroup);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "WSO2 gov-admin password-grant login failed for {ScimUsername}", scimUsername);
            return new Wso2GovAdminLoginResult(false, false);
        }
    }

    private async Task<bool> IsInGovAdminGroupAsync(string scimUsername, CancellationToken ct)
    {
        var filter = Uri.EscapeDataString($"userName eq \"{scimUsername}\"");
        using var listRequest = ScimRequest(HttpMethod.Get, $"/scim2/Users?filter={filter}");
        using var listResponse = await _http.SendAsync(listRequest, ct);
        if (!listResponse.IsSuccessStatusCode)
        {
            return false;
        }

        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync(ct));
        if (!listDoc.RootElement.TryGetProperty("Resources", out var resources) || resources.GetArrayLength() == 0)
        {
            return false;
        }

        var userId = resources[0].TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        using var userRequest = ScimRequest(HttpMethod.Get, $"/scim2/Users/{userId}");
        using var userResponse = await _http.SendAsync(userRequest, ct);
        if (!userResponse.IsSuccessStatusCode)
        {
            return false;
        }

        using var userDoc = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync(ct));
        if (!userDoc.RootElement.TryGetProperty("groups", out var groups))
        {
            return false;
        }

        foreach (var group in groups.EnumerateArray())
        {
            if (group.TryGetProperty("display", out var display)
                && string.Equals(display.GetString(), _govAdminOptions.GroupName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private HttpRequestMessage ScimRequest(HttpMethod method, string pathAndQuery)
    {
        var request = new HttpRequestMessage(method, _scimOptions.ScimBaseUrl.TrimEnd('/') + pathAndQuery);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_scimOptions.ScimAdminUsername}:{_scimOptions.ScimAdminPassword}")));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/scim+json"));
        return request;
    }
}
