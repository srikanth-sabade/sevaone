using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace SevaOne.Services.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _mux;

    public RedisHealthCheck(IConnectionMultiplexer mux)
    {
        _mux = mux;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!_mux.IsConnected)
        {
            return HealthCheckResult.Unhealthy("Redis connection multiplexer reports disconnected.");
        }

        try
        {
            var latency = await _mux.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Ping {latency.TotalMilliseconds:F0}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed.", ex);
        }
    }
}
