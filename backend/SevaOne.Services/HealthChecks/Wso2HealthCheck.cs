using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Services.HealthChecks;

/// <summary>
/// Reachability only, not a functional check -- hits WSO2 IS's public JWKS endpoint (no
/// credentials needed, no state changed). Any HTTP response, even 4xx, proves the server is up
/// and answering; only a connection-level failure (refused, timeout, DNS) counts as unhealthy.
/// </summary>
public sealed class Wso2HealthCheck : IHealthCheck
{
    private readonly HttpClient _http;
    private readonly Wso2CitizenIssuerOptions _options;

    public Wso2HealthCheck(HttpClient http, IOptions<Wso2CitizenIssuerOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _options.ScimBaseUrl.TrimEnd('/') + "/oauth2/jwks");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var response = await _http.SendAsync(request, cts.Token);
            return HealthCheckResult.Healthy($"WSO2 IS responded {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("WSO2 IS unreachable.", ex);
        }
    }
}
