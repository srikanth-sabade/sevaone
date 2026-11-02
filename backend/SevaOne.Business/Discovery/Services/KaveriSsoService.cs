using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// The citizen apply click for a Kaveri 2.0 service. We hold a platform sign-in to Kaveri, send Kaveri the citizen's profile,
/// our Kaveri service id and our application number, and get back a single sign-on address. The citizen is sent there.
/// Personal details are sent but not kept: only the service id and the address's host and path are recorded.
/// </summary>
public sealed partial class KaveriSsoService : IKaveriSsoService
{
    private readonly ITokenIntegrationRepository _integrations;
    private readonly IKaveriTokenService _tokens;
    private readonly ITokenStatusHttpClient _http;
    private readonly IAuditRepository _audit;
    private readonly IClock _clock;
    private readonly ILogger<KaveriSsoService> _logger;

    public KaveriSsoService(
        ITokenIntegrationRepository integrations, IKaveriTokenService tokens, ITokenStatusHttpClient http,
        IAuditRepository audit, IClock clock, ILogger<KaveriSsoService> logger)
    {
        _integrations = integrations;
        _tokens = tokens;
        _http = http;
        _audit = audit;
        _clock = clock;
        _logger = logger;
    }

    private const string Unavailable = "This service is not available right now. Please try again later.";

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public async Task<string> StartAsync(Citizen citizen, Scheme scheme, Department department, TokenIntegrationActiveRow integration, CancellationToken ct)
    {
        var kaveriServiceId = TokenStatusJson.ReadKaveriServiceId(integration.ExtraConfig);
        if (kaveriServiceId is null)
        {
            _logger.LogWarning("Kaveri service {Service} is switched on without a Kaveri service id.", scheme.ServiceCode);
            throw new ConflictException(Unavailable);
        }

        var (settings, _, problem) = await _tokens.LoadProviderAsync(ct);
        if (settings is null)
        {
            _logger.LogWarning("Kaveri sign-in is not ready: {Problem}", problem);
            throw new ConflictException(Unavailable);
        }

        var body = BuildRequest(citizen, settings, kaveriServiceId.Value, out var missing);
        if (missing.Count > 0)
            throw new DomainException($"Please add your {string.Join(", ", missing)} to your SevaOne profile, then apply again. The department needs it to sign you in.");

        var now = _clock.UtcNow;
        var reference = await _integrations.CreateAttemptAsync(new TokenIntegrationAttemptWrite
        {
            ServiceId = scheme.Id, DepartmentId = department.Id, CitizenId = citizen.Id, SevaOneUserId = citizen.SevaOneUserId,
            TokenId = null, AssistedMode = false, LandingUrl = settings.SsoUrl,
            SentFields = JsonSerializer.Serialize(new { edcsServiceId = kaveriServiceId.Value }),
            ExpiresAt = now.AddMinutes(30),
        }, ct);
        body["EdcsapplicationNumber"] = reference;
        var json = JsonSerializer.Serialize(body);

        var attemptId = (await _integrations.GetAttemptAsync(reference, ct))!.AttemptId;
        try
        {
            var reply = await CallAsync(settings, json, forceNewToken: false, ct);
            if (reply.HttpStatus == 401) reply = await CallAsync(settings, json, forceNewToken: true, ct);

            if (reply.Error is not null || reply.HttpStatus is not (>= 200 and < 300))
            {
                var why = reply.Error ?? KaveriTokenService.DescribeFailure("Kaveri's single sign-on", reply);
                await _integrations.FinishAttemptAsync(attemptId, null, "failed", Truncate(why, 250), ct);
                _logger.LogWarning("Kaveri single sign-on failed for {Reference}: {Why}", reference, why);
                if (reply.HttpStatus == 400)
                    throw new DomainException($"The department could not accept your details. {ErrorMessage(reply.Body) ?? "Please check your profile and try again."}");
                throw new ConflictException(Unavailable);
            }

            var redirect = ReadRedirect(reply.Body);
            if (redirect is null || !RedirectAllowed(redirect, settings.SsoUrl))
            {
                await _integrations.FinishAttemptAsync(attemptId, null, "failed", "Kaveri's reply had no acceptable sign-in address.", ct);
                _logger.LogWarning("Kaveri single sign-on for {Reference} returned no acceptable redirect address.", reference);
                throw new ConflictException(Unavailable);
            }

            // Kept: where the citizen was sent, without the session token in the address.
            var shown = new UriBuilder(redirect) { Query = "" }.Uri.GetLeftPart(UriPartial.Path);
            await _integrations.FinishAttemptAsync(attemptId, shown, "initiated", null, ct);

            await _audit.InsertAsync(new AuditLog
            {
                Id = Guid.NewGuid(), CitizenId = citizen.Id, Action = "apply.token_integration.sso_started",
                Resource = $"{scheme.Name} · {department.Name}", Detail = $"Single sign-on handoff started, application reference {reference}", At = now,
            }, ct);
            return redirect;
        }
        catch (Exception e) when (e is not (DomainException or ConflictException) && e is not OperationCanceledException)
        {
            await _integrations.FinishAttemptAsync(attemptId, null, "failed", Truncate(e.Message, 250), ct);
            throw;
        }
    }

    private async Task<TokenStatusHttpResult> CallAsync(KaveriSettings settings, string json, bool forceNewToken, CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync(forceNewToken, ct);
        if (token.AccessToken is null) return new TokenStatusHttpResult(token.HttpStatus, null, token.Error ?? "Could not sign in to Kaveri.");
        return await _http.PostJsonAsync(settings.SsoUrl, json, new TokenStatusAuth("bearer", null, null, token.AccessToken), ct);
    }

    /// <summary>The fields in the contract, filled from the citizen's profile. Optional ones are left out when we have nothing for them.
    /// "missing" lists, in plain words, what the citizen must add first.</summary>
    public static Dictionary<string, object?> BuildRequest(Citizen citizen, KaveriSettings settings, long kaveriServiceId, out List<string> missing)
    {
        missing = [];
        var name = citizen.Name?.Trim();
        if (string.IsNullOrEmpty(name)) missing.Add("name");

        var email = citizen.Email?.Trim();
        if (string.IsNullOrEmpty(email) || !EmailPattern().IsMatch(email)) missing.Add("email address");

        var mobile = NormalizeMobile(citizen.Mobile);
        if (mobile is null) missing.Add("10-digit mobile number");

        var gender = MapGender(citizen.Gender);
        if (gender is null) missing.Add("gender");

        var body = new Dictionary<string, object?>
        {
            ["full_name"] = name,
            ["email"] = email,
            ["mobile"] = mobile,
            ["emailVerified"] = settings.AssumeEmailVerified,
            ["gender"] = gender,
            ["EdcsserviceId"] = kaveriServiceId,
        };
        Optional(body, "digilockerid", citizen.DigiLockerId);
        Optional(body, "aadhaarHash", citizen.AadhaarHash);
        Optional(body, "Street", citizen.Address);
        Optional(body, "Loc", citizen.VillageName);
        Optional(body, "SubDistrict", citizen.Taluk);
        Optional(body, "District", citizen.District);
        Optional(body, "UserState", citizen.StateName);
        return body;
    }

    private static void Optional(Dictionary<string, object?> body, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) body[key] = value.Trim();
    }

    /// <summary>Ten digits, with a leading +91, 91 or 0 and any spaces or dashes removed.</summary>
    public static string? NormalizeMobile(string? mobile)
    {
        var digits = new string((mobile ?? "").Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91")) digits = digits[2..];
        else if (digits.Length == 11 && digits.StartsWith('0')) digits = digits[1..];
        return digits.Length == 10 && digits[0] is >= '6' and <= '9' ? digits : null;
    }

    /// <summary>Kaveri wants M, F or T.</summary>
    public static string? MapGender(string? gender) => gender?.Trim().ToLowerInvariant() switch
    {
        "m" or "male" => "M",
        "f" or "female" => "F",
        "t" or "transgender" or "third gender" or "other" or "others" => "T",
        _ => null,
    };

    private static string? ReadRedirect(string? body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body ?? "");
            if (doc.RootElement.TryGetProperty("success", out var ok) && ok.ValueKind == JsonValueKind.False) return null;
            return doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("redirectUrl", out var url) && url.ValueKind == JsonValueKind.String
                ? url.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    private static string? ErrorMessage(string? body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body ?? "");
            return doc.RootElement.TryGetProperty("errorMessage", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    /// <summary>The citizen's browser is sent to the address Kaveri returns, so it must be https and on Kaveri's own domain
    /// (the single sign-on address's host with its first label dropped, or the same host).</summary>
    public static bool RedirectAllowed(string redirect, string ssoUrl)
    {
        if (!Uri.TryCreate(redirect, UriKind.Absolute, out var target) || target.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(target.UserInfo)) return false;
        if (!Uri.TryCreate(ssoUrl, UriKind.Absolute, out var sso)) return false;

        var labels = sso.Host.Split('.');
        var allowedRoot = labels.Length >= 3 ? string.Join('.', labels.Skip(1)) : sso.Host;
        return target.Host.Equals(allowedRoot, StringComparison.OrdinalIgnoreCase)
            || target.Host.EndsWith("." + allowedRoot, StringComparison.OrdinalIgnoreCase)
            || target.Host.Equals(sso.Host, StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
