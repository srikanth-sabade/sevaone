using System.Diagnostics;
using System.Text.Json;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// Practice runs for a department building its side of the token integration. Practice ("sandbox") applications use the seeded test
/// citizen, carry the reference prefix EDCSTEST, never create an application record and are never checked by the status poller.
/// The test calls to the department's own addresses go out from this server, so they refuse internal addresses and cap time and size.
/// </summary>
public sealed class DeptPortalService : IDeptPortalService
{
    private const string TestCitizenSubject = "test-citizen-dept-integration-01";

    private readonly IDiscoveryRepository _discovery;
    private readonly IPortalRepository _portal;
    private readonly ITokenIntegrationRepository _integrations;
    private readonly IJwsTokenService _jws;
    private readonly IApplyRedirectProtector _protector;
    private readonly ITokenStatusHttpClient _http;
    private readonly ITokenVerificationService _verification;
    private readonly ITokenAcknowledgementService _acknowledgements;
    private readonly IClock _clock;

    public DeptPortalService(
        IDiscoveryRepository discovery, IPortalRepository portal, ITokenIntegrationRepository integrations, IJwsTokenService jws,
        IApplyRedirectProtector protector, ITokenStatusHttpClient http, ITokenVerificationService verification,
        ITokenAcknowledgementService acknowledgements, IClock clock)
    {
        _discovery = discovery;
        _portal = portal;
        _integrations = integrations;
        _jws = jws;
        _protector = protector;
        _http = http;
        _verification = verification;
        _acknowledgements = acknowledgements;
        _clock = clock;
    }

    public async Task<SandboxStartDto> StartSandboxAsync(TokenIntegrationCaller caller, string? serviceCode, CancellationToken ct)
    {
        var department = await _discovery.GetDepartmentByCodeAsync(caller.DepartmentCode, ct)
            ?? throw new NotFoundException("Department was not found.");

        Scheme service;
        if (!string.IsNullOrWhiteSpace(serviceCode))
        {
            service = await _discovery.GetSchemeByCodeAsync(department.Id, serviceCode.Trim(), ct)
                ?? throw new NotFoundException($"Service '{serviceCode}' was not found under your department.");
        }
        else
        {
            service = (await _discovery.ListSchemesAsync(department.Id, ct)).FirstOrDefault(s => s.IsActive)
                ?? throw new DomainException("Your department has no service to practise with yet.");
        }

        var citizen = await _portal.GetCitizenByWso2SubjectAsync(TestCitizenSubject, ct)
            ?? throw new NotFoundException("The practice citizen is not available on this server.");

        var now = _clock.UtcNow;
        var expires = now.AddMinutes(30);
        // Two practice runs in the same second would otherwise produce identical tokens (same user, service and times), so each carries
        // a random marker. It is only a practice token, so the extra claim does not matter to anything that reads it.
        var marker = "sandbox-" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
        var (tokenValue, tokenHash) = _jws.Mint(new JwsClaims(citizen.SevaOneUserId, department.Code, service.ServiceCode, false, marker, now, expires));
        var tokenId = Guid.NewGuid();
        await _portal.InsertTokenAsync(new SevaOneToken
        {
            Id = tokenId, CitizenId = citizen.Id, SevaOneUserId = citizen.SevaOneUserId, OperatorId = null, AssistedMode = false,
            DepartmentId = department.Id, SchemeId = service.Id, TokenValue = tokenValue, TokenHash = tokenHash, IssuedAt = now, ExpireAt = expires,
            MidnightValidUntil = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero), IsActive = true, ReturnUrl = "/dashboard",
        }, uow: null, ct);

        var departmentServiceId = string.IsNullOrWhiteSpace(service.DepartmentServiceId) ? service.ServiceCode : service.DepartmentServiceId!;
        var fields = new Dictionary<string, string>
        {
            ["UserId"] = citizen.SevaOneUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["assistedmode"] = "false",
            ["ServiceId"] = departmentServiceId,
        };
        // Optional fields appear only when this service is set up to send them, with made-up values: a practice run never carries real details.
        var setup = await _integrations.GetAsync(service.Id, ct);
        if (setup?.SendMobile == true) fields["mobile"] = "9000000000";
        if (setup?.SendDigilockerId == true) fields["digilockerid"] = "DL-SANDBOX";

        var reference = await _integrations.CreateAttemptAsync(new TokenIntegrationAttemptWrite
        {
            ServiceId = service.Id, DepartmentId = department.Id, CitizenId = citizen.Id, SevaOneUserId = citizen.SevaOneUserId, TokenId = tokenId,
            AssistedMode = false, LandingUrl = "sandbox", SentFields = JsonSerializer.Serialize(fields), ExpiresAt = expires, IsSandbox = true,
        }, ct);

        fields["edcsApplicationRefNo"] = reference;
        fields["Token"] = _protector.Protect(tokenValue);

        return new SandboxStartDto(reference, service.ServiceCode, departmentServiceId, expires, fields,
            "A practice application. It uses a test citizen, never appears on a real dashboard, and expires in 30 minutes.");
    }

    public async Task<TokenVerifyResult> VerifySandboxAsync(TokenVerifyRequest request, TokenIntegrationCaller caller, CancellationToken ct)
    {
        var reference = request.EdcsApplicationRefNo?.Trim();
        var info = string.IsNullOrEmpty(reference) ? null : await _integrations.GetVerifyInfoAsync(reference, ct);
        if (info is not null && !info.IsSandbox) return new TokenVerifyResult(false, "not_a_sandbox_application");
        return await _verification.VerifyAsync(request, caller, ct);
    }

    public async Task<(TokenAckOutcome Outcome, bool NotSandbox)> AcknowledgeSandboxAsync(
        TokenAckRequest request, TokenIntegrationCaller caller, string? callerIp, CancellationToken ct)
    {
        var reference = request.EdcsApplicationRefNo?.Trim();
        var attempt = string.IsNullOrEmpty(reference) ? null : await _integrations.GetAttemptAsync(reference, ct);
        if (attempt is not null && !attempt.IsSandbox) return (new TokenAckOutcome(null, "not a sandbox application"), true);
        return (await _acknowledgements.AcknowledgeAsync(request, caller, callerIp, ct), false);
    }

    public async Task<LandingTestResult> TestLandingAsync(TokenIntegrationCaller caller, string url, string? serviceCode, CancellationToken ct)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new DomainException("Enter the full address of your landing page, starting with https://");

        var start = await StartSandboxAsync(caller, serviceCode, ct);
        var watch = Stopwatch.StartNew();
        var reply = await _http.PostFormAsync(uri.ToString(), start.Fields, ct);
        watch.Stop();

        // A reply that is simply long (a full page) is still a reply: only the status code matters here.
        var ok = reply.HttpStatus == 200;
        var note = ok
            ? "Your page answered 200. Remember the real hand-off comes from the citizen's browser, not from SevaOne's server."
            : reply.HttpStatus is { } code ? $"Your page answered HTTP {code}. It must answer 200." : "SevaOne could not reach your page.";
        return new LandingTestResult(start.EdcsApplicationRefNo, reply.HttpStatus, ok, watch.ElapsedMilliseconds,
            reply.HttpStatus is null ? reply.Error : null, start.Fields.Keys.Where(k => k != "Token").Append("Token").ToList(), note);
    }

    public async Task<TokenStatusTestResult> TestStatusAsync(StatusTestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Url) || string.IsNullOrWhiteSpace(request.Number) || request.Number.Length > 100)
            throw new DomainException("Enter the address of your status API and a number to ask about.");
        var field = string.IsNullOrWhiteSpace(request.RequestField) ? "GscNumber" : request.RequestField.Trim();
        if (field.Length > 50 || !field.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) throw new DomainException("The field name may only contain letters, digits and underscores.");
        var authType = (request.AuthType ?? "none").Trim().ToLowerInvariant();
        if (authType is not ("none" or "basic" or "bearer" or "header")) throw new DomainException("Choose none, basic, bearer or header authentication.");
        var statusPath = string.IsNullOrWhiteSpace(request.StatusPath) ? "status" : request.StatusPath.Trim();

        var body = JsonSerializer.Serialize(new Dictionary<string, string> { [field] = request.Number.Trim() });
        var reply = await _http.PostJsonAsync(request.Url.Trim(), body,
            new TokenStatusAuth(authType, request.AuthUser, request.AuthHeaderName, request.Secret), ct);

        if (reply.Error is not null) return new TokenStatusTestResult(reply.HttpStatus, null, null, null, null, reply.Error);
        string? status = null, message = null, error = null;
        if (reply.HttpStatus is not (>= 200 and < 300)) error = $"Your API answered HTTP {reply.HttpStatus}. It should answer 200 with the status.";
        else
        {
            try
            {
                using var doc = JsonDocument.Parse(reply.Body ?? "");
                status = TokenStatusJson.ReadString(doc.RootElement, statusPath);
                message = TokenStatusJson.ReadString(doc.RootElement, request.MessagePath);
                if (string.IsNullOrWhiteSpace(status)) error = $"No status was found in the reply at '{statusPath}'.";
            }
            catch (JsonException) { error = "Your API's reply is not JSON."; }
        }
        var raw = reply.Body is { Length: > 2000 } ? reply.Body[..2000] + "…" : reply.Body;
        return new TokenStatusTestResult(reply.HttpStatus, raw, status, message, null, error);
    }
}
