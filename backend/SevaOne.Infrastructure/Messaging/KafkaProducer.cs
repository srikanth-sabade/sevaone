using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.Messaging;

/// <summary>
/// Lazily builds the real Confluent producer only when Kafka.Enabled is true and a broker is
/// configured — local/dev runs without Kafka installed by falling back to a log line per
/// message, so OutboxDispatcher behaves the same either way.
/// </summary>
public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly Lazy<IProducer<string, string>?> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _options = options.Value;
        _logger = logger;
        _producer = new Lazy<IProducer<string, string>?>(BuildProducer);
    }

    private IProducer<string, string>? BuildProducer()
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            return new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                ClientId = _options.ClientId,
                Acks = Acks.All,
                MessageTimeoutMs = 5000
            }).Build();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka producer could not start; falling back to log-only publish.");
            return null;
        }
    }

    public async Task ProduceAsync(string topic, string key, string value, CancellationToken ct)
    {
        var producer = _producer.Value;
        if (producer is null)
        {
            _logger.LogInformation("Outbox {Topic} {Key}: {Value}", topic, key, value);
            return;
        }

        await producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = value }, ct);
    }

    public void Dispose()
    {
        if (_producer.IsValueCreated)
        {
            _producer.Value?.Dispose();
        }
    }
}
