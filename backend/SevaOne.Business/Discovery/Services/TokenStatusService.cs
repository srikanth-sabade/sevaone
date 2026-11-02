using System.Text.Json;
using Microsoft.Extensions.Logging;
using SevaOne.Business.Common;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// Status check of the token integration (Seva Sindhu V3, URL 3). We post {"GscNumber": "..."} to the
/// department's status API, read the status out of its answer using the service's saved settings, and bring the
/// application on the citizen's dashboard up to date. The same application list the rest of SevaOne uses, so
/// nothing separate is needed on the dashboard.
/// </summary>
public sealed class TokenStatusService : ITokenStatusService
{
    /// <summary>A citizen opening the page again does not make us ask the department again for this long.</summary>
    private static readonly TimeSpan MinimumGap = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BackgroundGap = TimeSpan.FromMinutes(30);
    private const int MaxRawBodyShown = 2000;

    private readonly ITokenIntegrationRepository _integrations;
    private readonly IDiscoveryRepository _discovery;
    private readonly IOrchestrationRepository _orchestration;
    private readonly IStatusRepository _status;
    private readonly ITokenStatusHttpClient _http;
    private readonly ITokenStatusSecretProtector _secrets;
    private readonly IKaveriTokenService _kaveri;
    private readonly TokenApplicationWriter _writer;
    private readonly ICurrentCitizen _current;
    private readonly IClock _clock;
    private readonly ILogger<TokenStatusService> _logger;

    public TokenStatusService(
        ITokenIntegrationRepository integrations, IDiscoveryRepository discovery, IOrchestrationRepository orchestration,
        IStatusRepository status, ITokenStatusHttpClient http, ITokenStatusSecretProtector secrets, IKaveriTokenService kaveri,
        TokenApplicationWriter writer, ICurrentCitizen current, IClock clock, ILogger<TokenStatusService> logger)
    {
        _integrations = integrations;
        _discovery = discovery;
        _orchestration = orchestration;
        _status = status;
        _http = http;
        _secrets = secrets;
        _kaveri = kaveri;
        _writer = writer;
        _current = current;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TokenStatusRefreshDto?> RefreshForCitizenAsync(string reference, CancellationToken ct)
    {
        var citizenId = _current.CitizenId ?? throw new ForbiddenException("Not authenticated.");
        var target = await _integrations.GetStatusTargetAsync(reference, ct);
        if (target is null || target.CitizenId != citizenId) return null;
        return await RefreshAsync(target, respectGap: true, ct);
    }

    public async Task<int> PollDueAsync(int limit, CancellationToken ct)
    {
        var references = await _integrations.ListStatusDueAsync(BackgroundGap, limit, ct);
        var asked = 0;
        foreach (var reference in references)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var target = await _integrations.GetStatusTargetAsync(reference, ct);
                if (target is null) continue;
                await RefreshAsync(target, respectGap: false, ct);
                asked++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Status check failed for {Reference}", reference);
            }
        }
        return asked;
    }

    public async Task<TokenStatusTestResult> TestAsync(string departmentCode, string serviceCode, string gscNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(gscNumber) || gscNumber.Length > 100) throw new DomainException("Enter a GSC number to test with (100 characters at most).");

        var department = await _discovery.GetDepartmentByCodeAsync(departmentCode, ct)
            ?? throw new NotFoundException($"Department '{departmentCode}' was not found.");
        var service = await _discovery.GetSchemeByCodeAsync(department.Id, serviceCode, ct)
            ?? throw new NotFoundException($"Service '{serviceCode}' was not found under '{departmentCode}'.");
        var setup = await _integrations.GetAsync(service.Id, ct);
        if (setup?.TypeKey is null) throw new DomainException("Save this service's integration first.");

        var statusUrl = setup.StatusUrl;
        if (string.IsNullOrWhiteSpace(statusUrl) && setup.TypeKey == KaveriTokenService.TypeKey)
            statusUrl = (await _kaveri.LoadProviderAsync(ct)).Settings?.StatusUrl;
        if (string.IsNullOrWhiteSpace(statusUrl))
            throw new DomainException(setup.TypeKey == KaveriTokenService.TypeKey
                ? "Save the Kaveri settings (including the status address) first." : "Save a status check URL for this service first.");

        var config = TokenStatusJson.Read(setup.ExtraConfig);
        var ciphertext = setup.HasStatusSecret ? await _integrations.GetStatusSecretCiphertextAsync(service.Id, ct) : null;
        var result = await CallAsync(statusUrl, config, ciphertext, gscNumber.Trim(), ct);
        return new TokenStatusTestResult(
            result.HttpStatus, Truncate(result.RawBody), result.StatusText, result.Message, result.Mapped?.ToString(), result.Error);
    }

    private async Task<TokenStatusRefreshDto> RefreshAsync(TokenStatusTargetRow target, bool respectGap, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        if (respectGap && target.StatusCheckedAt is { } last && now - last < MinimumGap)
            return new TokenStatusRefreshDto(false, target.DepartmentStatusText, null, false);

        var config = TokenStatusJson.Read(target.ExtraConfig);
        var number = config.NumberSource == "edcs" ? target.EdcsApplicationRefNo : target.DepartmentApplicationRefNo;
        var result = string.IsNullOrWhiteSpace(number)
            ? new CallResult(null, null, null, null, null, "There is no application number to ask the department about yet.")
            : await CallAsync(target.StatusUrl, config, target.StatusSecretCiphertext, number, ct);
        await _integrations.RecordStatusCheckAsync(target.AttemptId, result.StatusText, result.Error is null ? null : Truncate(result.Error, 300), ct);

        // The department may not have the application yet (the citizen has not finished on its site): not an error, just nothing to show.
        if (result.NotFound) return new TokenStatusRefreshDto(true, target.DepartmentStatusText, null, false);

        if (result.Error is not null)
        {
            _logger.LogInformation("Status check for {Reference} did not succeed: {Error}", target.EdcsApplicationRefNo, result.Error);
            return new TokenStatusRefreshDto(true, target.DepartmentStatusText, null, true);
        }

        if (target.ApplicationId is { } applicationId)
        {
            await ApplyAsync(target, applicationId, result, now, ct);
        }
        else
        {
            // First sign that the department has the application (single sign-on has no acknowledgement): put it on the dashboard.
            var note = $"Status from the department: {result.StatusText}" + (string.IsNullOrWhiteSpace(result.Message) ? "." : $". {result.Message}");
            await _writer.CreateAsync(new TokenApplicationNew(
                target.CitizenId, target.ServiceId, target.DepartmentId, target.DepartmentCode, target.ServiceName, target.EdcsApplicationRefNo,
                target.DepartmentApplicationRefNo, result.Mapped ?? ApplicationStatus.Submitted, now, target.SakalaDays, note,
                "application.token_integration.status_found", null), ct);
        }

        return new TokenStatusRefreshDto(true, result.StatusText, result.Message, false);
    }

    private async Task ApplyAsync(TokenStatusTargetRow target, Guid applicationId, CallResult result, DateTimeOffset now, CancellationToken ct)
    {
        var current = (ApplicationStatus)(target.ApplicationStatus ?? 0);
        var note = $"Status from the department: {result.StatusText}" + (string.IsNullOrWhiteSpace(result.Message) ? "." : $". {result.Message}");

        if (result.Mapped is { } mapped && mapped != current)
        {
            await _orchestration.UpdateApplicationStatusAsync(applicationId, mapped, ct);
            await _status.InsertApplicationEventAsync(new ApplicationEvent
            {
                Id = Guid.NewGuid(), ApplicationId = applicationId, Status = mapped, Note = note, At = now,
            }, ct);
            await _orchestration.InsertOutboxMessageAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Topic = "application.status_changed",
                Payload = JsonSerializer.Serialize(new { sevaReference = target.EdcsApplicationRefNo, status = ApplicationStatusMapper.ToApi(mapped) }),
                CreatedAt = now,
            }, uow: null, ct);
            return;
        }

        // The department used a word we have no mapping for, or it is the status we already have. Record a
        // changed word on the timeline without moving the application; a repeat of the same word adds nothing.
        if (!string.Equals(target.DepartmentStatusText, result.StatusText, StringComparison.Ordinal))
        {
            await _status.InsertApplicationEventAsync(new ApplicationEvent
            {
                Id = Guid.NewGuid(), ApplicationId = applicationId, Status = current, Note = note, At = now,
            }, ct);
        }
    }

    private sealed record CallResult(int? HttpStatus, string? RawBody, string? StatusText, string? Message, ApplicationStatus? Mapped, string? Error, bool NotFound = false);

    private async Task<CallResult> CallAsync(string statusUrl, TokenStatusConfig config, string? secretCiphertext, string number, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new Dictionary<string, string> { [string.IsNullOrWhiteSpace(config.RequestField) ? "GscNumber" : config.RequestField] = number });

        TokenStatusHttpResult reply;
        if (config.AuthType == "provider")
        {
            // The type's own sign-in (Kaveri). A refused token is renewed once and the call repeated.
            reply = await CallWithProviderTokenAsync(statusUrl, body, forceNew: false, ct);
            if (reply.HttpStatus == 401) reply = await CallWithProviderTokenAsync(statusUrl, body, forceNew: true, ct);
        }
        else
        {
            string? secret = null;
            if (secretCiphertext is not null)
            {
                secret = _secrets.TryUnprotect(secretCiphertext);
                if (secret is null) return new CallResult(null, null, null, null, null, "The saved secret for this API could not be read. Enter it again.");
            }
            reply = await _http.PostJsonAsync(statusUrl, body, new TokenStatusAuth(config.AuthType, config.AuthUser, config.AuthHeaderName, secret), ct);
        }

        if (reply.Error is not null) return new CallResult(reply.HttpStatus, reply.Body, null, null, null, reply.Error);
        if (reply.HttpStatus == 404 && config.AuthType == "provider") return new CallResult(404, reply.Body, null, null, null, "Not found at the department yet.", NotFound: true);
        if (reply.HttpStatus is not (>= 200 and < 300)) return new CallResult(reply.HttpStatus, reply.Body, null, null, null, $"The department answered HTTP {reply.HttpStatus}.");

        try
        {
            using var document = JsonDocument.Parse(reply.Body ?? "");
            var statusText = TokenStatusJson.ReadString(document.RootElement, config.StatusPath)?.Trim();
            if (string.IsNullOrEmpty(statusText))
                return new CallResult(reply.HttpStatus, reply.Body, null, null, null, $"No status was found in the reply at '{config.StatusPath}'.");
            if (statusText.Length > 200) statusText = statusText[..200];

            var message = TokenStatusJson.ReadString(document.RootElement, config.MessagePath)?.Trim();
            if (message is { Length: > 300 }) message = message[..300];
            return new CallResult(reply.HttpStatus, reply.Body, statusText, message, TokenStatusJson.Map(config, statusText), null);
        }
        catch (JsonException)
        {
            return new CallResult(reply.HttpStatus, reply.Body, null, null, null, "The department's reply is not JSON.");
        }
    }

    private async Task<TokenStatusHttpResult> CallWithProviderTokenAsync(string statusUrl, string body, bool forceNew, CancellationToken ct)
    {
        var token = await _kaveri.GetAccessTokenAsync(forceNew, ct);
        if (token.AccessToken is null) return new TokenStatusHttpResult(token.HttpStatus, null, token.Error ?? "Could not sign in to the department.");
        return await _http.PostJsonAsync(statusUrl, body, new TokenStatusAuth("bearer", null, null, token.AccessToken), ct);
    }

    private static string? Truncate(string? value, int max = MaxRawBodyShown) =>
        value is null || value.Length <= max ? value : value[..max] + "…";
}
