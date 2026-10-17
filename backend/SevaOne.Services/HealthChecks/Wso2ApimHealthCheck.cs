using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SevaOne.Services.HealthChecks;

/// <summary>
/// Reachability check for the internal WSO2 API Manager gateway. A response from the gateway,
/// including a redirect or a protected 4xx, proves the APIM process is answering; only a
/// connection-level failure is unhealthy.
/// </summary>
public sealed class Wso2ApimHealthCheck : IHealthCheck
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public Wso2ApimHealthCheck(
        HttpClient http,
        IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var baseUrl = _configuration["Wso2Apim:BaseUrl"] ?? "https://wso2apim:8243";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + "/");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var response = await _http.SendAsync(request, cts.Token);
            return HealthCheckResult.Healthy($"WSO2 APIM responded {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("WSO2 APIM unreachable.", ex);
        }
    }
}
