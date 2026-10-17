using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Connectors;

public sealed class SakalaClient : ISakalaClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SakalaOptions _options;
    private readonly ILogger<SakalaClient> _log;

    public SakalaClient(HttpClient http, IOptions<SakalaOptions> options, ILogger<SakalaClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<SakalaAckRecord?> GetAckDataAsync(string gscNo, CancellationToken ct)
    {
        try
        {
            var url = _options.BaseUrl.TrimEnd('/') + "/api/Gscmobile/GetSakalaackdata";
            using var response = await _http.PostAsJsonAsync(url, new RawRequest(gscNo), Json, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Confirmed live: an unrecognized gscno returns HTTP 500 with a raw ADO.NET
                // exception message ("Cannot find table 0") rather than a clean 404/empty
                // array -- this is the actual "not found" signal for this API, not a fault on
                // our side, so it's logged at Info (not Error) and simply returns null.
                _log.LogInformation("Sakala ack lookup for {GscNo} returned {Status} -- treating as not found", gscNo, (int)response.StatusCode);
                return null;
            }

            var rows = await response.Content.ReadFromJsonAsync<List<RawRecord>>(Json, ct);
            var row = rows?.FirstOrDefault();
            return row is null ? null : Map(row);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Sakala ack lookup failed for {GscNo}", gscNo);
            return null;
        }
    }

    private static SakalaAckRecord Map(RawRecord row) => new()
    {
        GscNo = row.SakalaNo ?? "",
        ApplicantName = row.ApplicantName ?? "",
        ServiceName = row.ServiceName ?? "",
        InstitutionName = row.InstitutionName ?? "",
        OfficerName = NullIfBlank(row.OfficerName),
        SecretariatDepartmentName = NullIfBlank(row.SecretariatDepartmentName),
        SakalaDate = ParseDate(row.SakalaDate),
        SakalaClosedDate = ParseDate(row.SakalaClosedDate),
        DueDate = ParseDate(row.DueDate),
        DaysPending = ParseInt(row.NumberOfDaysPending),
        StatusCode = row.StatusCode ?? "",
        StatusDescription = row.StatusDescription ?? "",
        FirstComplaint = MapComplaint(row.FirstComplaintNo, row.FirstComplaintDate, row.FirstComplaintClosedDate,
            row.FirstComplaintDueDate, row.FirstComplaintStatusCode, row.FirstComplaintStatusDescription, row.FirstComplaintDaysPending),
        SecondComplaint = MapComplaint(row.SecondComplaintNo, row.SecondComplaintDate, row.SecondComplaintClosedDate,
            row.SecondComplaintDueDate, row.SecondComplaintStatusCode, row.SecondComplaintStatusDescription, row.SecondComplaintDaysPending),
    };

    private static SakalaComplaint? MapComplaint(
        string? no, string? date, string? closedDate, string? dueDate, string? statusCode, string? statusDescription, string? daysPending)
    {
        if (string.IsNullOrWhiteSpace(no)) return null;
        return new SakalaComplaint
        {
            ComplaintNo = no,
            ComplaintDate = ParseDate(date),
            ClosedDate = ParseDate(closedDate),
            DueDate = ParseDate(dueDate),
            StatusCode = NullIfBlank(statusCode),
            StatusDescription = NullIfBlank(statusDescription),
            DaysPending = ParseInt(daysPending),
        };
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    // The real API's dates are "dd/MM/yyyy" strings, or "" when not applicable (e.g. an unused
    // second complaint) -- never a JSON null.
    private static DateOnly? ParseDate(string? raw) =>
        !string.IsNullOrWhiteSpace(raw) && DateOnly.TryParseExact(raw, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;

    private static int? ParseInt(string? raw) =>
        !string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var n) ? n : null;

    private sealed record RawRequest([property: JsonPropertyName("gscno")] string GscNo);

    // Property names mirror the real API's literal (space-containing) JSON keys via
    // JsonPropertyName -- confirmed live against sakala.kar.nic.in/ack_api/api/Gscmobile/GetSakalaackdata.
    private sealed class RawRecord
    {
        [JsonPropertyName("SAKALA NO")] public string? SakalaNo { get; set; }
        [JsonPropertyName("APPLICANT NAME")] public string? ApplicantName { get; set; }
        [JsonPropertyName("SERVICE NAME")] public string? ServiceName { get; set; }
        [JsonPropertyName("INSTITUTION NAME")] public string? InstitutionName { get; set; }
        [JsonPropertyName("OFFICER NAME")] public string? OfficerName { get; set; }
        [JsonPropertyName("SECRETARIAT DEPARTMENT NAME")] public string? SecretariatDepartmentName { get; set; }
        [JsonPropertyName("SAKALA DATE")] public string? SakalaDate { get; set; }
        [JsonPropertyName("SAKALA CLOSED DATE")] public string? SakalaClosedDate { get; set; }
        [JsonPropertyName("DUE DATE")] public string? DueDate { get; set; }
        [JsonPropertyName("NUMBER OF DAYS PENDING")] public string? NumberOfDaysPending { get; set; }
        [JsonPropertyName("STATUS CODE")] public string? StatusCode { get; set; }
        [JsonPropertyName("STATUS DESCRIPTION")] public string? StatusDescription { get; set; }

        [JsonPropertyName("FIRST COMPLAINT NO")] public string? FirstComplaintNo { get; set; }
        [JsonPropertyName("FIRST COMPLAINT DATE")] public string? FirstComplaintDate { get; set; }
        [JsonPropertyName("FIRST COMPLAINT CLOSED DATE")] public string? FirstComplaintClosedDate { get; set; }
        [JsonPropertyName("FIRST COMPLAINT DUE DATE")] public string? FirstComplaintDueDate { get; set; }
        [JsonPropertyName("FIRST COMPLAINT STATUS CODE")] public string? FirstComplaintStatusCode { get; set; }
        [JsonPropertyName("FIRST COMPLAINT STATUS DESCRIPTION")] public string? FirstComplaintStatusDescription { get; set; }
        [JsonPropertyName("FIRST COMPLAINT NUMBER OF DAYS PENDING")] public string? FirstComplaintDaysPending { get; set; }

        [JsonPropertyName("SECOND COMPLAINT NO")] public string? SecondComplaintNo { get; set; }
        [JsonPropertyName("SECOND COMPLAINT DATE")] public string? SecondComplaintDate { get; set; }
        [JsonPropertyName("SECOND COMPLAINT CLOSED DATE")] public string? SecondComplaintClosedDate { get; set; }
        [JsonPropertyName("SECOND COMPLAINT DUE DATE")] public string? SecondComplaintDueDate { get; set; }
        [JsonPropertyName("SECOND COMPLAINT STATUS CODE")] public string? SecondComplaintStatusCode { get; set; }
        [JsonPropertyName("SECOND COMPLAINT STATUS DESCRIPTION")] public string? SecondComplaintStatusDescription { get; set; }
        [JsonPropertyName("SECOND COMPLAINT NUMBER OF DAYS PENDING")] public string? SecondComplaintDaysPending { get; set; }
    }
}
