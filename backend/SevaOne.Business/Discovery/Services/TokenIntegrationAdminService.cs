using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Domain.Enums;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

public sealed class TokenIntegrationAdminService : ITokenIntegrationAdminService
{
    private const int MaxIps = 50;

    private readonly IDiscoveryRepository _discovery;
    private readonly ITokenIntegrationRepository _integrations;
    private readonly ICatalogVisibilityRepository _changeLog;
    private readonly ITokenStatusSecretProtector _secrets;
    private readonly IKaveriTokenService _kaveri;

    public TokenIntegrationAdminService(
        IDiscoveryRepository discovery, ITokenIntegrationRepository integrations, ICatalogVisibilityRepository changeLog,
        ITokenStatusSecretProtector secrets, IKaveriTokenService kaveri)
    {
        _secrets = secrets;
        _kaveri = kaveri;
        _discovery = discovery;
        _integrations = integrations;
        _changeLog = changeLog;
    }

    public async Task<IReadOnlyList<TokenIntegrationTypeDto>> ListTypesAsync(CancellationToken ct) =>
        (await _integrations.ListTypesAsync(ct))
            .Select(t => new TokenIntegrationTypeDto(t.TypeKey, t.DisplayName, t.Description, t.FieldsJson)).ToList();

    public async Task<IReadOnlyList<TokenIntegrationListItemDto>> ListServicesAsync(
        string? search, string? departmentCode, bool configuredOnly, bool citizenVisibleOnly, int limit, CancellationToken ct)
    {
        var rows = await _integrations.ListServicesAsync(search?.Trim(), departmentCode?.Trim(), configuredOnly, citizenVisibleOnly, limit, ct);
        return rows.Select(r => new TokenIntegrationListItemDto(
            r.ServiceId, r.DepartmentCode, r.DepartmentName, r.ServiceCode, r.Name, r.NameKannada,
            r.CitizenVisible, r.OperatorMethod, r.TypeKey, r.TypeKey is not null, r.IsEnabled ?? false, r.LandingUrl)).ToList();
    }

    public async Task<TokenIntegrationDto> GetAsync(string departmentCode, string serviceCode, CancellationToken ct)
    {
        var service = await ResolveServiceAsync(departmentCode, serviceCode, ct);
        return await LoadAsync(service.Id, ct);
    }

    public async Task<TokenIntegrationDto> SaveAsync(
        string departmentCode, string serviceCode, TokenIntegrationWriteRequest request, string changedBy, CancellationToken ct)
    {
        var service = await ResolveServiceAsync(departmentCode, serviceCode, ct);

        var types = await _integrations.ListTypesAsync(ct);
        if (!types.Any(t => t.TypeKey == request.TypeKey))
            throw new DomainException($"Unknown integration type '{request.TypeKey}'.");

        var isKaveri = request.TypeKey == KaveriTokenService.TypeKey;
        // Seva Sindhu's own "redirect" API sends the citizen on to the right department page itself, so unlike
        // "edcs_api_form" (where we post to the department ourselves, so its URL is required), "edcs_api" needs no
        // department URL from us at all -- just the Seva Sindhu service id below. Both share that service id and
        // the optional extra keys, since both call the same Seva Sindhu /generate.
        var isEdcsApi = request.TypeKey == EdcsApiTokenService.TypeKey;
        var isEdcsApiFamily = isEdcsApi || request.TypeKey == EdcsApiFormTokenService.TypeKey;
        var before = await _integrations.GetAsync(service.Id, ct);

        // Single sign-on has no fixed landing page, status address or department IP list of its own: those come from the
        // type's shared settings or from the other system for each citizen.
        var landingUrl = isKaveri ? null : NormalizeUrl(request.LandingUrl, "Department URL");
        var statusUrl = isKaveri ? null : NormalizeUrl(request.StatusUrl, "Status check URL");
        if (statusUrl is not null && HostIsInternal(statusUrl))
            throw new DomainException("Status check URL must be a public department address, not a local or private one.");
        if (request.IsEnabled && !isKaveri && !isEdcsApi && landingUrl is null)
            throw new DomainException("A department URL is required before this integration can be switched on.");

        long? kaveriServiceId = null;
        if (isKaveri)
        {
            kaveriServiceId = request.KaveriServiceId ?? TokenStatusJson.ReadKaveriServiceId(before?.ExtraConfig);
            if (kaveriServiceId is <= 0) throw new DomainException("The Kaveri service id must be a positive number.");
            if (request.IsEnabled)
            {
                if (kaveriServiceId is null) throw new DomainException("Enter the Kaveri service id before switching this service on.");
                if (!(await ProviderStateAsync(request.TypeKey, ct)).IsReady) throw new DomainException("Save the Kaveri settings and password first (Provider settings).");
            }
        }

        string? sevasindhuServiceId = null;
        EdcsKeys? edcsKeys = null;
        if (isEdcsApiFamily)
        {
            sevasindhuServiceId = request.SevasindhuServiceId?.Trim();
            if (!string.IsNullOrEmpty(sevasindhuServiceId) && (!int.TryParse(sevasindhuServiceId, out var n) || n <= 0))
                throw new DomainException("The Seva Sindhu service id must be a positive whole number.");
            if (request.IsEnabled && string.IsNullOrEmpty(sevasindhuServiceId) && string.IsNullOrEmpty(before?.OperatorSevasindhuId))
                throw new DomainException("Enter the Seva Sindhu service id before switching this service on.");

            foreach (var (raw, name) in new[] { (request.EdcsKey1, "Key1"), (request.EdcsKey2, "Key2"), (request.EdcsKey3, "Key3"), (request.EdcsKey4, "Key4"), (request.EdcsKey5, "Key5") })
                if (EdcsKeyValue.Validate(raw, name) is { } keyError) throw new DomainException(keyError);

            edcsKeys = new EdcsKeys(
                BlankToNull(request.EdcsKey1), BlankToNull(request.EdcsKey2), BlankToNull(request.EdcsKey3),
                BlankToNull(request.EdcsKey4), BlankToNull(request.EdcsKey5));
        }

        var ips = isKaveri ? [] : NormalizeIps(request.AllowedIps);

        var statusConfig = request.StatusConfig is null
            ? (before?.TypeKey == request.TypeKey ? TokenStatusJson.Read(before.ExtraConfig) : DefaultStatusConfig(request.TypeKey))
            : ValidateStatusConfig(request.StatusConfig, isKaveri);
        if (isKaveri && statusConfig.AuthType != "provider") statusConfig = statusConfig with { AuthType = "provider", AuthUser = null, AuthHeaderName = null };
        var hasSecret = before?.HasStatusSecret == true;
        string? secretCiphertext = null; // keep what is stored
        if (request.StatusSecret is { } newSecret)
        {
            if (newSecret.Length > 500) throw new DomainException("The status API secret is too long.");
            secretCiphertext = newSecret.Length == 0 ? "" : _secrets.Protect(newSecret);
            hasSecret = newSecret.Length > 0;
        }
        if (statusConfig.AuthType is not ("none" or "provider") && !hasSecret)
            throw new DomainException("Enter the secret for the status API (or choose no authentication).");
        if (statusUrl is null && statusConfig.AuthType is not ("none" or "provider"))
            throw new DomainException("Authentication is set but there is no status check URL.");

        await _integrations.UpsertAsync(new TokenIntegrationWrite
        {
            ServiceId = service.Id,
            TypeKey = request.TypeKey,
            LandingUrl = landingUrl,
            SendMobile = request.SendMobile,
            SendDigilockerId = request.SendDigilockerId,
            StatusUrl = statusUrl,
            AllowedIps = ips,
            IsEnabled = request.IsEnabled,
            ExtraConfig = TokenStatusJson.Write(statusConfig, kaveriServiceId, edcsKeys),
            StatusSecretCiphertext = secretCiphertext,
        }, changedBy, ct);

        if (isEdcsApiFamily && !string.IsNullOrEmpty(sevasindhuServiceId))
            await _integrations.SetSevasindhuServiceIdAsync(service.Id, sevasindhuServiceId, ct);

        await _changeLog.InsertServiceChangeLogAsync(new ServiceChangeLog
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            ChangedBy = changedBy,
            ChangeType = "updated",
            Diff = JsonSerializer.Serialize(new
            {
                tokenIntegration = new
                {
                    typeKey = new { old = before?.TypeKey, @new = request.TypeKey },
                    landingUrl = new { old = before?.LandingUrl, @new = landingUrl },
                    statusUrl = new { old = before?.StatusUrl, @new = statusUrl },
                    isEnabled = new { old = before?.IsEnabled ?? false, @new = request.IsEnabled },
                    statusAuth = statusConfig.AuthType,
                    kaveriServiceId,
                    sevasindhuServiceId = isEdcsApiFamily ? new { old = before?.OperatorSevasindhuId, @new = sevasindhuServiceId } : null,
                    statusPath = statusConfig.StatusPath,
                    statusSecretChanged = request.StatusSecret is not null,
                },
            }),
        }, uow: null, ct);

        return await LoadAsync(service.Id, ct);
    }

    public async Task RemoveAsync(string departmentCode, string serviceCode, string changedBy, CancellationToken ct)
    {
        var service = await ResolveServiceAsync(departmentCode, serviceCode, ct);
        if (!await _integrations.DeleteAsync(service.Id, ct)) return;

        await _changeLog.InsertServiceChangeLogAsync(new ServiceChangeLog
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            ChangedBy = changedBy,
            ChangeType = "updated",
            Diff = JsonSerializer.Serialize(new { tokenIntegration = "removed" }),
        }, uow: null, ct);
    }

    public async Task<TokenProviderDto> GetProviderAsync(string typeKey, CancellationToken ct)
    {
        RequireKaveri(typeKey);
        var state = await ProviderStateAsync(typeKey, ct);
        return new TokenProviderDto(typeKey, state.Settings, state.HasSecret, state.IsReady, state.UpdatedAt, state.UpdatedBy);
    }

    public async Task<TokenProviderDto> SaveProviderAsync(string typeKey, TokenProviderWriteRequest request, string changedBy, CancellationToken ct)
    {
        RequireKaveri(typeKey);
        var k = request.Kaveri ?? throw new DomainException("Kaveri settings are required.");
        var settings = new KaveriSettings(
            RequiredUrl(k.LoginUrl, "Sign-in address"), OptionalUrl(k.RefreshUrl, "Refresh address") ?? "",
            RequiredUrl(k.SsoUrl, "Single sign-on address"), OptionalUrl(k.StatusUrl, "Status address") ?? "",
            (k.Username ?? "").Trim(), k.AssumeEmailVerified);
        if (settings.Username.Length is 0 or > 100) throw new DomainException("Enter the technical username (100 characters at most).");

        string? secret = null; // keep what is stored
        if (request.Password is { } password)
        {
            if (password.Length > 200) throw new DomainException("The password is too long.");
            secret = password.Length == 0 ? "" : _secrets.Protect(password);
        }
        var existing = await _integrations.GetProviderAsync(typeKey, ct);
        if (secret is null && string.IsNullOrEmpty(existing?.SecretCiphertext)) throw new DomainException("Enter the technical login's password.");

        await _integrations.SaveProviderAsync(typeKey, JsonSerializer.Serialize(settings, new JsonSerializerOptions(JsonSerializerDefaults.Web)), secret, changedBy, ct);
        return await GetProviderAsync(typeKey, ct);
    }

    public Task<TokenProviderTestResult> TestProviderLoginAsync(string typeKey, CancellationToken ct)
    {
        RequireKaveri(typeKey);
        return _kaveri.TestLoginAsync(ct);
    }

    private static void RequireKaveri(string typeKey)
    {
        if (typeKey != KaveriTokenService.TypeKey) throw new NotFoundException($"There are no shared settings for '{typeKey}'.");
    }

    private sealed record ProviderState(KaveriSettings? Settings, bool HasSecret, bool IsReady, DateTimeOffset? UpdatedAt, string? UpdatedBy);

    private async Task<ProviderState> ProviderStateAsync(string typeKey, CancellationToken ct)
    {
        var row = await _integrations.GetProviderAsync(typeKey, ct);
        if (row is null) return new ProviderState(null, false, false, null, null);
        var settings = KaveriTokenService.ReadSettings(row.SettingsJson);
        var hasSecret = !string.IsNullOrEmpty(row.SecretCiphertext);
        var ready = hasSecret && settings.LoginUrl.Length > 0 && settings.SsoUrl.Length > 0 && settings.Username.Length > 0;
        return new ProviderState(settings, hasSecret, ready, row.UpdatedAt, row.UpdatedBy);
    }

    private static string RequiredUrl(string? value, string label) =>
        OptionalUrl(value, label) ?? throw new DomainException($"{label} is required.");

    private static string? OptionalUrl(string? value, string label)
    {
        var url = NormalizeUrl(value, label);
        if (url is not null && HostIsInternal(url)) throw new DomainException($"{label} must be a public address, not a local or private one.");
        return url;
    }

    public async Task<IReadOnlyList<TokenIntegrationCredentialDto>> ListCredentialsAsync(string departmentCode, CancellationToken ct)
    {
        var department = await ResolveDepartmentAsync(departmentCode, ct);
        return (await _integrations.ListCredentialsAsync(department.Id, ct))
            .Select(c => new TokenIntegrationCredentialDto(c.Id, c.Username, c.IsActive, c.CreatedAt, c.CreatedBy, c.PasswordChangedAt, c.LastUsedAt)).ToList();
    }

    public async Task<TokenIntegrationNewCredentialDto> CreateCredentialAsync(string departmentCode, string createdBy, CancellationToken ct)
    {
        var department = await ResolveDepartmentAsync(departmentCode, ct);
        var password = TokenIntegrationSecrets.GeneratePassword();
        var hash = TokenIntegrationSecrets.Hash(password);
        var prefix = new string(department.Code.ToLowerInvariant().Where(c => char.IsAsciiLetterOrDigit(c) || c == '-').Take(48).ToArray());

        // The random suffix keeps logins unique; check it is free before using it.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var username = $"tok-{prefix}-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(3)).ToLowerInvariant()}";
            if (await _integrations.GetCredentialAsync(username, ct) is not null) continue;
            var id = await _integrations.CreateCredentialAsync(department.Id, username, hash, createdBy, ct);
            return new TokenIntegrationNewCredentialDto(id, username, password);
        }
        throw new DomainException("Could not create a login. Please try again.");
    }

    public async Task<TokenIntegrationNewCredentialDto> ResetCredentialAsync(string departmentCode, Guid credentialId, CancellationToken ct)
    {
        var department = await ResolveDepartmentAsync(departmentCode, ct);
        var existing = (await _integrations.ListCredentialsAsync(department.Id, ct)).FirstOrDefault(c => c.Id == credentialId)
            ?? throw new NotFoundException("Login was not found for this department.");
        var password = TokenIntegrationSecrets.GeneratePassword();
        await _integrations.UpdateCredentialAsync(credentialId, department.Id, TokenIntegrationSecrets.Hash(password), null, ct);
        return new TokenIntegrationNewCredentialDto(credentialId, existing.Username, password);
    }

    public async Task SetCredentialActiveAsync(string departmentCode, Guid credentialId, bool isActive, CancellationToken ct)
    {
        var department = await ResolveDepartmentAsync(departmentCode, ct);
        if (!await _integrations.UpdateCredentialAsync(credentialId, department.Id, null, isActive, ct))
            throw new NotFoundException("Login was not found for this department.");
    }

    private async Task<Department> ResolveDepartmentAsync(string departmentCode, CancellationToken ct) =>
        await _discovery.GetDepartmentByCodeAsync(departmentCode, ct)
            ?? throw new NotFoundException($"Department '{departmentCode}' was not found.");

    private async Task<TokenIntegrationDto> LoadAsync(Guid serviceId, CancellationToken ct)
    {
        var row = await _integrations.GetAsync(serviceId, ct)
            ?? throw new NotFoundException("Service was not found.");

        var configured = row.TypeKey is not null;
        var warnings = new List<string>();
        if (configured && row.TypeKey == KaveriTokenService.TypeKey)
        {
            if (TokenStatusJson.ReadKaveriServiceId(row.ExtraConfig) is null) warnings.Add("No Kaveri service id yet: this service cannot be switched on without it.");
            if (!(await ProviderStateAsync(row.TypeKey, ct)).IsReady) warnings.Add("The Kaveri settings are not complete yet (see Provider settings).");
        }
        else if (configured && row.TypeKey == EdcsApiTokenService.TypeKey)
        {
            // Seva Sindhu's own "redirect" API sends the citizen on from here, so no department URL of ours is needed.
            if (string.IsNullOrEmpty(row.StatusUrl))
                warnings.Add("No status check URL: we cannot look up an application's status ourselves.");
        }
        else if (configured)
        {
            if (row.LandingUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true)
                warnings.Add("The department URL is not https. User details and the token would travel unencrypted.");
            if (string.IsNullOrEmpty(row.StatusUrl))
                warnings.Add("No status check URL: we cannot look up an application's status ourselves.");
            if (row.AllowedIps is not { Length: > 0 })
                warnings.Add("No department server IPs: the acknowledgement API cannot be restricted to this department yet.");
        }

        var edcsKeys = TokenStatusJson.ReadEdcsKeys(row.ExtraConfig);
        return new TokenIntegrationDto(
            row.ServiceId, row.DepartmentCode, row.DepartmentName, row.ServiceCode, row.Name,
            row.OperatorMethod, row.OperatorSevasindhuId,
            configured, row.TypeKey, row.LandingUrl, row.SendMobile ?? false, row.SendDigilockerId ?? false,
            row.StatusUrl, TokenStatusJson.Read(row.ExtraConfig), row.HasStatusSecret, TokenStatusJson.ReadKaveriServiceId(row.ExtraConfig),
            row.TypeKey == KaveriTokenService.TypeKey && (await ProviderStateAsync(row.TypeKey, ct)).IsReady,
            row.AllowedIps ?? [], row.IsEnabled, row.UpdatedAt, row.UpdatedBy, row.Version, warnings,
            edcsKeys.Key1, edcsKeys.Key2, edcsKeys.Key3, edcsKeys.Key4, edcsKeys.Key5);
    }

    private async Task<Scheme> ResolveServiceAsync(string departmentCode, string serviceCode, CancellationToken ct)
    {
        var department = await _discovery.GetDepartmentByCodeAsync(departmentCode, ct)
            ?? throw new NotFoundException($"Department '{departmentCode}' was not found.");
        return await _discovery.GetSchemeByCodeAsync(department.Id, serviceCode, ct)
            ?? throw new NotFoundException($"Service '{serviceCode}' was not found under '{departmentCode}'.");
    }

    private static readonly Regex PathPattern = new(@"^\$?\.?[A-Za-z0-9_\-]+(\[\d+\])*(\.[A-Za-z0-9_\-]+(\[\d+\])*)*$", RegexOptions.Compiled);
    private static readonly Regex HeaderPattern = new(@"^[A-Za-z0-9\-]{1,64}$", RegexOptions.Compiled);
    private static readonly string[] ReservedHeaders = ["host", "content-length", "content-type", "transfer-encoding", "connection", "authorization"];

    private static readonly Regex FieldPattern = new(@"^[A-Za-z][A-Za-z0-9_]{0,49}$", RegexOptions.Compiled);

    private static TokenStatusConfig DefaultStatusConfig(string typeKey) => typeKey == KaveriTokenService.TypeKey
        ? new TokenStatusConfig("provider", null, null, "data.Status", null, null, "ApplicationNumber", "edcs")
        : new TokenStatusConfig();

    private static TokenStatusConfig ValidateStatusConfig(TokenStatusConfig config, bool isKaveri)
    {
        var authType = (config.AuthType ?? "none").Trim().ToLowerInvariant();
        if (!TokenStatusJson.AuthTypes.Contains(authType)) throw new DomainException("Choose how the status API is authenticated: none, basic, bearer or header.");
        if (authType == "provider" && !isKaveri) throw new DomainException("Sign-in through the integration type applies to single sign-on services only.");
        var requestField = string.IsNullOrWhiteSpace(config.RequestField) ? "GscNumber" : config.RequestField.Trim();
        if (!FieldPattern.IsMatch(requestField)) throw new DomainException("The name of the field we send must be letters, digits and underscores, such as GscNumber or ApplicationNumber.");
        var numberSource = string.IsNullOrWhiteSpace(config.NumberSource) ? "department" : config.NumberSource.Trim().ToLowerInvariant();
        if (numberSource is not ("department" or "edcs")) throw new DomainException("Choose which application number to send: the department's or ours.");

        string? user = null, header = null;
        if (authType == "basic")
        {
            user = config.AuthUser?.Trim();
            if (string.IsNullOrEmpty(user) || user.Length > 100) throw new DomainException("Basic authentication needs a username (100 characters at most).");
        }
        if (authType == "header")
        {
            header = config.AuthHeaderName?.Trim();
            if (header is null || !HeaderPattern.IsMatch(header) || ReservedHeaders.Contains(header.ToLowerInvariant()))
                throw new DomainException("Header authentication needs a header name such as X-Api-Key (letters, digits and dashes).");
        }

        var statusPath = (config.StatusPath ?? "").Trim();
        if (statusPath.Length is 0 or > 200 || !PathPattern.IsMatch(statusPath))
            throw new DomainException("Where to find the status in the reply must look like status, data.state or result[0].status.");
        var messagePath = config.MessagePath?.Trim();
        if (!string.IsNullOrEmpty(messagePath) && (messagePath.Length > 200 || !PathPattern.IsMatch(messagePath)))
            throw new DomainException("Where to find the message in the reply must look like message or data.remarks.");

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in config.StatusMap ?? new Dictionary<string, string>())
        {
            var word = key.Trim();
            if (word.Length is 0 or > 100) throw new DomainException("A department status word must be 1 to 100 characters.");
            if (!Enum.TryParse<ApplicationStatus>(value, ignoreCase: true, out var mapped) || !Enum.IsDefined(mapped))
                throw new DomainException($"'{value}' is not one of our statuses (Submitted, DocumentVerification, UnderReview, Approved, Rejected).");
            map[word] = mapped.ToString();
        }
        if (map.Count > 50) throw new DomainException("At most 50 status words can be mapped.");

        return new TokenStatusConfig(authType, user, header, statusPath, string.IsNullOrEmpty(messagePath) ? null : messagePath, map, requestField, numberSource);
    }

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeUrl(string? value, string label)
    {
        var url = value?.Trim();
        if (string.IsNullOrEmpty(url)) return null;
        if (url.Length > 500) throw new DomainException($"{label} is too long (500 characters at most).");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) || url.Any(char.IsWhiteSpace))
            throw new DomainException($"{label} must be a full web address starting with https:// (or http://), with no spaces.");
        if (!string.IsNullOrEmpty(uri.UserInfo))
            throw new DomainException($"{label} must not contain a username or password.");
        return url;
    }

    private static bool HostIsInternal(string url)
    {
        var uri = new Uri(url);
        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (!IPAddress.TryParse(uri.Host, out var ip)) return false;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal || IPAddress.IsLoopback(ip);
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || b[0] == 127 || b[0] == 0;
    }

    private static string[] NormalizeIps(IReadOnlyList<string>? input)
    {
        var result = new List<string>();
        foreach (var raw in input ?? [])
        {
            var item = raw.Trim();
            if (item.Length == 0) continue;
            var parts = item.Split('/');
            var ok = parts.Length <= 2 && IPAddress.TryParse(parts[0], out var ip);
            if (ok && parts.Length == 2)
            {
                var max = IPAddress.Parse(parts[0]).AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
                ok = int.TryParse(parts[1], out var bits) && bits >= 0 && bits <= max;
            }
            if (!ok) throw new DomainException($"'{item}' is not a valid IP address or range (like 10.1.2.3 or 10.1.2.0/24).");
            if (!result.Contains(item, StringComparer.OrdinalIgnoreCase)) result.Add(item);
        }
        if (result.Count > MaxIps) throw new DomainException($"At most {MaxIps} IP addresses can be listed.");
        return result.ToArray();
    }
}
