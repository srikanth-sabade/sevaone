using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Infrastructure.ExternalHttp;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// The department reports its acknowledgement to the Seva Sindhu EP0115 API, not to us, so we ask for it:
///   1. POST /acknowledgment {EdcsApplicationRefNo}: 404 until the department has reported; then the file type and the file (base64).
///   2. POST /pending-service-charges {UserId, ServiceId}: the department's own reference number, payment status, fee and dates,
///      matched on the application reference. If the list has no such entry, POST /service-charge-lookup {ApplicationRefNo}.
/// With both, the call is handed to the same acknowledgement code the department's own call would reach, which stores it and
/// puts the application on the citizen's dashboard. Without a department reference nothing is recorded and we ask again later.
/// </summary>
public sealed class EdcsApiAckService : IEdcsApiAckService
{
    private static readonly TimeSpan Gap = TimeSpan.FromMinutes(2);
    private static readonly TokenStatusAuth NoAuth = new("none", null, null, null);

    private readonly ITokenIntegrationRepository _integrations;
    private readonly ITokenStatusHttpClient _http;
    private readonly ITokenAcknowledgementService _acknowledgements;
    private readonly EdcsApiOptions _options;
    private readonly ILogger<EdcsApiAckService> _logger;

    public EdcsApiAckService(
        ITokenIntegrationRepository integrations, ITokenStatusHttpClient http, ITokenAcknowledgementService acknowledgements,
        IOptions<EdcsApiOptions> options, ILogger<EdcsApiAckService> logger)
    {
        _integrations = integrations;
        _http = http;
        _acknowledgements = acknowledgements;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> PollDueAsync(int limit, CancellationToken ct)
    {
        var baseUrl = _options.BaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl)) return 0;

        var acknowledged = 0;
        foreach (var due in await _integrations.ListAckDueAsync(Gap, limit, ct))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // Marked first: a department whose API is slow or failing is asked again after the gap, not on every pass.
                await _integrations.RecordAckCheckAsync(due.AttemptId, ct);
                if (await TryAcknowledgeAsync(baseUrl, due, ct)) acknowledged++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Acknowledgement check failed for {Reference}", due.EdcsApplicationRefNo);
            }
        }
        return acknowledged;
    }

    private async Task<bool> TryAcknowledgeAsync(string baseUrl, TokenAckDueRow due, CancellationToken ct)
    {
        var reference = due.EdcsApplicationRefNo;

        // Only references the Seva Sindhu API issued ("EDCS" + its service id + digits). Our own local references use a different
        // numbering that can coincide with other applications' references over there; looking those up would hand a citizen
        // someone else's acknowledgement.
        if (string.IsNullOrEmpty(due.SevasindhuServiceId) || !reference.StartsWith("EDCS" + due.SevasindhuServiceId, StringComparison.Ordinal))
        {
            _logger.LogWarning("Reference {Reference} was not issued by the Seva Sindhu API for service {Service}; not looked up.", reference, due.SevasindhuServiceId);
            return false;
        }

        var ack = await PostAsync($"{baseUrl}/acknowledgment", new { EdcsApplicationRefNo = reference }, ct);
        if (ack.HttpStatus == 404) return false;          // the department has not reported yet
        if (ack.Error is not null || ack.HttpStatus is not (>= 200 and < 300) || ack.Body is null)
        {
            _logger.LogWarning("Seva Sindhu acknowledgment lookup for {Reference}: HTTP {Status} {Error}", reference, ack.HttpStatus, ack.Error);
            return false;
        }
        var file = ReadObject(ack.Body);
        var fileBase64 = Get(file, "Acknowledgment");
        if (file is null || string.IsNullOrWhiteSpace(fileBase64)) return false;
        if (Get(file, "EdcsApplicationRefNo") is { Length: > 0 } echoed && !string.Equals(echoed, reference, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Seva Sindhu answered for {Echoed} when asked about {Reference}; ignored.", echoed, reference);
            return false;
        }

        var details = await FindDepartmentDetailsAsync(baseUrl, due, ct);
        var deptRef = Get(details, "DepartmentApplicationRefNo");
        // If the answer says whose application it is, it must be this citizen's.
        if (Get(details, "UserId") is { Length: > 0 } owner && owner != due.SevaOneUserId.ToString(CultureInfo.InvariantCulture))
        {
            _logger.LogWarning("Seva Sindhu's record for {Reference} belongs to a different user; ignored.", reference);
            return false;
        }
        if (string.IsNullOrWhiteSpace(deptRef))
        {
            _logger.LogWarning("Acknowledgement for {Reference} is held by Seva Sindhu but its department reference could not be read; will ask again.", reference);
            return false;
        }

        var request = new TokenAckRequest
        {
            UserId = due.SevaOneUserId.ToString(CultureInfo.InvariantCulture),
            EdcsApplicationRefNo = reference,
            DepartmentApplicationRefNo = deptRef,
            DepartmentPaymentStatus = Get(details, "DepartmentPaymentStatus"),
            DepartmentFeeCollectionDate = Get(details, "DepartmentFeeCollectionDate"),
            DepartmentFee = GetDecimal(details, "DepartmentFee"),
            DepartmentPaymentRefNumber = Get(details, "DepartmentPaymentRefNumber"),
            AcknowledgmentFileType = Get(file, "AcknowledgmentFileType") ?? Get(details, "AcknowledgmentFileType"),
            AcknowledgmentDate = Get(details, "AcknowledgmentDate"),
            Acknowledgment = fileBase64,
        };
        // Same recording as the department's own call; the department was identified by the application it belongs to.
        var outcome = await _acknowledgements.AcknowledgeAsync(request, new TokenIntegrationCaller(Guid.Empty, due.DepartmentId, due.DepartmentCode), callerIp: null, ct);
        if (!outcome.IsSuccess)
            _logger.LogWarning("Acknowledgement for {Reference} was fetched but not accepted: {Reason}", reference, outcome.Reason);
        return outcome.IsSuccess;
    }

    /// <summary>The department's reference, payment details and dates: from the user's list of applications, else from the
    /// single-application lookup.</summary>
    private async Task<JsonElement?> FindDepartmentDetailsAsync(string baseUrl, TokenAckDueRow due, CancellationToken ct)
    {
        var listed = await PostAsync($"{baseUrl}/pending-service-charges",
            new { UserId = due.SevaOneUserId.ToString(CultureInfo.InvariantCulture), ServiceId = due.SevasindhuServiceId }, ct);
        if (listed.Body is not null && listed.HttpStatus is >= 200 and < 300 && ReadObject(listed.Body) is { } list)
        {
            foreach (var key in new[] { "Applications", "applications" })
                if (list.TryGetProperty(key, out var items) && items.ValueKind == JsonValueKind.Array)
                    foreach (var item in items.EnumerateArray())
                        if (string.Equals(Get(item, "EdcsApplicationRefNo"), due.EdcsApplicationRefNo, StringComparison.OrdinalIgnoreCase))
                            return item;
        }

        var lookup = await PostAsync($"{baseUrl}/service-charge-lookup", new { ApplicationRefNo = due.EdcsApplicationRefNo }, ct);
        return lookup.Body is not null && lookup.HttpStatus is >= 200 and < 300 ? ReadObject(lookup.Body) : null;
    }

    private Task<TokenStatusHttpResult> PostAsync(string url, object body, CancellationToken ct) =>
        _http.PostJsonAsync(url, JsonSerializer.Serialize(body), NoAuth, ct);

    private static JsonElement? ReadObject(string json)
    {
        try
        {
            var root = JsonDocument.Parse(json).RootElement.Clone();
            return root.ValueKind == JsonValueKind.Object ? root : null;
        }
        catch (JsonException) { return null; }
    }

    /// <summary>A property by name, ignoring case; numbers are read as text.</summary>
    private static string? Get(JsonElement? obj, string name)
    {
        if (obj is not { ValueKind: JsonValueKind.Object } o) return null;
        foreach (var p in o.EnumerateObject())
        {
            if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            return p.Value.ValueKind switch
            {
                JsonValueKind.String => p.Value.GetString(),
                JsonValueKind.Number => p.Value.GetRawText(),
                _ => null,
            };
        }
        return null;
    }

    private static decimal? GetDecimal(JsonElement? obj, string name) =>
        decimal.TryParse(Get(obj, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
}
