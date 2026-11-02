using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SevaOne.Infrastructure.Messaging;

namespace SevaOne.Services.HealthChecks;

/// <summary>Kafka is optional (see KafkaProducer) -- when Kafka:Enabled is false, this reports Healthy rather than trying to reach a broker that was never meant to be there.</summary>
public sealed class KafkaHealthCheck : IHealthCheck
{
    private readonly KafkaOptions _options;

    public KafkaHealthCheck(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Kafka disabled — not applicable."));
        }

        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _options.BootstrapServers }).Build();
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));
            return Task.FromResult(metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy($"{metadata.Brokers.Count} broker(s) reachable.")
                : HealthCheckResult.Unhealthy("No Kafka brokers returned by metadata request."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka broker unreachable.", ex));
        }
    }
}
