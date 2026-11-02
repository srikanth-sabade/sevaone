namespace SevaOne.Infrastructure.Messaging;

/// <summary>Thin wrapper so callers (OutboxDispatcher) don't need to know Confluent.Kafka's API. When Kafka.Enabled is false, the implementation just logs — see KafkaProducer.</summary>
public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string key, string value, CancellationToken ct);
}
