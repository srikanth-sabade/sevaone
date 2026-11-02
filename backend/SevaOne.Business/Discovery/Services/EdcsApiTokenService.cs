using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// Token integration where Seva Sindhu issues the token. Seva Sindhu's EP0115 API: POST /generate (UserId, ServiceId = the
/// Seva Sindhu service id, AssistedMode Y/N, optionally Mobile/DigiLockerId/Key1..Key5) returns a signed token and the
/// application reference "EDCS" + service id + 8 digits. The department's entry page checks that token against the
/// same API, so a token made by us would be refused. We then send the citizen's browser to Seva Sindhu's own GET
/// /redirect?token=... address: it reads the service id out of the token and forwards the browser to the right
/// department page itself, so we never need to know or store that department's URL.
/// </summary>
public sealed class EdcsApiTokenService : IEdcsApiTokenService
{
    public const string TypeKey = "edcs_api";
    private const string Unavailable = "This service is not available right now. Please try again later.";

    private readonly ITokenIntegrationRepository _integrations;
    private readonly ITokenStatusHttpClient _http;
    private readonly IAuditRepository _audit;
    private readonly IClock _clock;
    private readonly EdcsApiOptions _options;
    private readonly ILogger<EdcsApiTokenService> _logger;

    public EdcsApiTokenService(
        ITokenIntegrationRepository integrations, ITokenStatusHttpClient http, IAuditRepository audit, IClock clock,
        IOptions<EdcsApiOptions> options, ILogger<EdcsApiTokenService> logger)
    {
        _integrations = integrations;
        _http = http;
        _audit = audit;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApplyOutcomeDto> StartAsync(Citizen citizen, Scheme scheme, Department department, TokenIntegrationActiveRow integration, CancellationToken ct, IReadOnlyDictionary<string, string>? answers = null)
    {
        var baseUrl = _options.BaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogWarning("TokenIntegration:EdcsApi:BaseUrl is not set, so service {Service} cannot start.", scheme.ServiceCode);
            throw new ConflictException(Unavailable);
        }
        if (!int.TryParse(scheme.SevasindhuServiceId, NumberStyles.None, CultureInfo.InvariantCulture, out var sevasindhuServiceId) || sevasindhuServiceId <= 0)
        {
            _logger.LogWarning("Service {Service} uses the Seva Sindhu token API but has no Seva Sindhu service id in the catalogue.", scheme.ServiceCode);
            throw new ConflictException(Unavailable);
        }

        var userId = citizen.SevaOneUserId.ToString(CultureInfo.InvariantCulture);
        var request = new Dictionary<string, object?>
        {
            ["UserId"] = userId, ["ServiceId"] = sevasindhuServiceId, ["AssistedMode"] = "N", ["AssistedModeEnabled"] = false,
        };
        // Departments ask for different extras and the API ignores what it does not need, so send every identity field we hold.
        if (!string.IsNullOrWhiteSpace(citizen.Mobile)) request["Mobile"] = citizen.Mobile;
        if (!string.IsNullOrWhiteSpace(citizen.Email)) request["EmailId"] = citizen.Email;
        if (!string.IsNullOrWhiteSpace(citizen.DigiLockerId)) request["DigiLockerId"] = citizen.DigiLockerId;

        // A handful of departments need an extra value (for example an Aadhaar hash) in one of Key1..Key5.
        // Most services leave all of these blank -- set in the admin panel only when a department asks for one.
        var raw = EdcsKeyValue.RawKeys(integration.ExtraConfig);
        var (values, needs) = EdcsKeyValue.Build(raw, citizen, answers);
        // Ask first: no token is made until every question the service asks has an answer.
        if (needs.Count > 0) return new ApplyOutcomeDto(RequiresLogin: false, ApplyUrl: null, ExpiresAt: null, IsHomepageFallback: false, PostForm: null, NeedsInput: needs);
        foreach (var (key, value) in values) request[key] = value;

        var reply = await _http.PostJsonAsync($"{baseUrl}/generate", JsonSerializer.Serialize(request), new TokenStatusAuth("none", null, null, null), ct);
        if (reply.Error is not null || reply.HttpStatus is not (>= 200 and < 300) || string.IsNullOrWhiteSpace(reply.Body))
        {
            _logger.LogWarning("Seva Sindhu token API refused for service {Service}: HTTP {Status} {Error}", scheme.ServiceCode, reply.HttpStatus, reply.Error ?? Truncate(reply.Body, 200));
            throw new ConflictException(Unavailable);
        }

        string? token, reference;
        int expiresInSeconds;
        try
        {
            using var doc = JsonDocument.Parse(reply.Body);
            token = Read(doc.RootElement, "AccessToken");
            reference = Read(doc.RootElement, "EdcsApplicationRefNo");
            expiresInSeconds = doc.RootElement.TryGetProperty("ExpiresIn", out var e) && e.TryGetInt32(out var n) ? n : 1800;
        }
        catch (JsonException)
        {
            _logger.LogWarning("Seva Sindhu token API returned something that is not JSON for service {Service}.", scheme.ServiceCode);
            throw new ConflictException(Unavailable);
        }
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(reference) || reference.Length > 32)
        {
            _logger.LogWarning("Seva Sindhu token API reply for service {Service} had no usable token or reference.", scheme.ServiceCode);
            throw new ConflictException(Unavailable);
        }

        var now = _clock.UtcNow;
        var applyUrl = $"{baseUrl}/redirect?token={Uri.EscapeDataString(token)}";

        // Kept for the record: what we asked Seva Sindhu for, without the token itself.
        var kept = request.Where(f => f.Key != "Token").ToDictionary(f => f.Key, f => f.Value?.ToString() ?? "");
        await _integrations.CreateAttemptAsync(new TokenIntegrationAttemptWrite
        {
            ServiceId = scheme.Id, DepartmentId = department.Id, CitizenId = citizen.Id, SevaOneUserId = citizen.SevaOneUserId,
            TokenId = null, AssistedMode = false, LandingUrl = applyUrl, SentFields = JsonSerializer.Serialize(kept),
            ExpiresAt = now.AddSeconds(expiresInSeconds), EdcsApplicationRefNo = reference,
        }, ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id, Action = "apply.token_integration.started",
            Resource = $"{scheme.Name} · {department.Name}", Detail = $"Token from Seva Sindhu issued, application reference {reference}", At = now,
        }, ct);

        return new ApplyOutcomeDto(RequiresLogin: false, ApplyUrl: applyUrl, ExpiresAt: now.AddSeconds(expiresInSeconds), IsHomepageFallback: false,
            PostForm: null);
    }

    private static string? Read(JsonElement root, string name)
    {
        foreach (var p in root.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String) return p.Value.GetString();
        return null;
    }

    private static string Truncate(string? s, int max) => string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max];
}
