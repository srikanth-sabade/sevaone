using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SevaOne.Domain.Connection;

namespace SevaOne.Services.HealthChecks;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _factory;

    public PostgresHealthCheck(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var conn = _factory.CreateConnection();
            await ((DbConnection)conn).OpenAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres connection failed.", ex);
        }
    }
}
