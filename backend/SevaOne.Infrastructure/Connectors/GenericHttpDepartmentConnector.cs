using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SevaOne.Infrastructure.Connectors;

/// <summary>Talks to the department-hub stub over HTTP using a `{departmentCode}/...` path prefix — used for every integrated department except revenue, which has its own dedicated base URL (see RevenueDepartmentConnector).</summary>
public sealed class GenericHttpDepartmentConnector : IDepartmentConnector
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public GenericHttpDepartmentConnector(string departmentCode, HttpClient http)
    {
        DepartmentCode = departmentCode;
        _http = http;
    }

    public string DepartmentCode { get; }
    public bool IsIntegrated => true;

    public async Task<DepartmentSessionHandle> OpenSessionAsync(CitizenContext citizen, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync(
            $"{DepartmentCode}/sessions",
            new { citizenSub = citizen.Wso2Subject, name = citizen.Name, mobile = citizen.Mobile },
            Json,
            ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(Json, ct)
                   ?? throw new InvalidOperationException("Empty department session response.");
        return new DepartmentSessionHandle(DepartmentCode, body.AccessToken, body.ExpiresAt);
    }

    public Task<IReadOnlyList<RemoteScheme>> ListSchemesAsync(DepartmentSessionHandle session, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RemoteScheme>>(Array.Empty<RemoteScheme>());

    public async Task<SubmitResult> SubmitAsync(DepartmentSessionHandle session, SubmitApplicationCommand cmd, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{DepartmentCode}/applications")
        {
            Content = JsonContent.Create(cmd, options: Json)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SubmitResponse>(Json, ct)
                   ?? throw new InvalidOperationException("Empty submit response.");
        return new SubmitResult(body.DepartmentRef, body.Status, body.Note);
    }

    public async Task<RemoteStatus> GetStatusAsync(DepartmentSessionHandle session, string departmentRef, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{DepartmentCode}/applications/{Uri.EscapeDataString(departmentRef)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StatusResponse>(Json, ct)
                   ?? throw new InvalidOperationException("Empty status response.");
        return new RemoteStatus(body.DepartmentRef, body.Status, body.Note);
    }

    public async Task CloseSessionAsync(DepartmentSessionHandle session, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{DepartmentCode}/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await _http.SendAsync(request, ct);
        _ = response;
    }

    public async Task<DigiLockerFetchResult> FetchDocumentAsync(DepartmentSessionHandle session, string documentType, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{DepartmentCode}/digilocker/fetch")
        {
            Content = JsonContent.Create(new { documentType }, options: Json)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DigiLockerResponse>(Json, ct)
                   ?? new DigiLockerResponse(documentType, "DL-STUB", true, "Fetched from DigiLocker stub.");
        return new DigiLockerFetchResult(body.DocumentType, body.DocumentId, body.Found, body.Note);
    }

    public async Task<PaymentResult> InitiatePaymentAsync(DepartmentSessionHandle session, PaymentRequest request, CancellationToken ct)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{DepartmentCode}/payments")
        {
            Content = JsonContent.Create(request, options: Json)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(Json, ct)
                   ?? new PaymentResponse("PAY-STUB", "Khajane-II", "Success", "Payment stub accepted.");
        return new PaymentResult(body.PaymentId, body.Gateway, body.Status, body.Note);
    }

    public async Task<ESignResult> RequestESignAsync(DepartmentSessionHandle session, ESignRequest request, CancellationToken ct)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{DepartmentCode}/esign")
        {
            Content = JsonContent.Create(request, options: Json)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ESignResponse>(Json, ct)
                   ?? new ESignResponse("ESIGN-STUB", "Signed", "C-DAC e-Sign stub.");
        return new ESignResult(body.RequestId, body.Status, body.Note);
    }

    private sealed record SessionResponse(string AccessToken, DateTimeOffset ExpiresAt);
    private sealed record SubmitResponse(string DepartmentRef, string Status, string Note);
    private sealed record StatusResponse(string DepartmentRef, string Status, string Note);
    private sealed record DigiLockerResponse(string DocumentType, string DocumentId, bool Found, string Note);
    private sealed record PaymentResponse(string PaymentId, string Gateway, string Status, string Note);
    private sealed record ESignResponse(string RequestId, string Status, string Note);
}
