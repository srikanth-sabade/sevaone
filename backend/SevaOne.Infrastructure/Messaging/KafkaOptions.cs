namespace SevaOne.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "sevaone";

    /// <summary>How many failed publish attempts before OutboxDispatcher stops retrying a message.</summary>
    public int MaxAttempts { get; set; } = 20;
}
