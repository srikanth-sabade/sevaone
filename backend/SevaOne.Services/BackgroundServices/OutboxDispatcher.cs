using Microsoft.Extensions.Options;
using SevaOne.Infrastructure.Messaging;
using SevaOne.Infrastructure.Repositories.Orchestration;

namespace SevaOne.Services.BackgroundServices;

/// <summary>Drains orchestration.outbox_messages and publishes each row to Kafka (or logs it when Kafka is disabled) — see IEventPublisher for why writes go through the outbox table instead of publishing directly.</summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly KafkaOptions _options;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopes, IOptions<KafkaOptions> options, ILogger<OutboxDispatcher> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Outbox publish cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrchestrationRepository>();
        var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

        var pending = await repository.GetPendingOutboxMessagesAsync(_options.MaxAttempts, batchSize: 50, ct);
        foreach (var message in pending)
        {
            try
            {
                await producer.ProduceAsync(message.Topic, message.Id.ToString(), message.Payload, ct);
                await repository.MarkOutboxPublishedAsync(message.Id, DateTimeOffset.UtcNow, ct);
            }
            catch (Exception ex)
            {
                await repository.MarkOutboxFailedAsync(message.Id, message.Attempts + 1, ex.Message, ct);
                _logger.LogWarning(ex, "Failed to publish outbox {Id}", message.Id);
            }
        }
    }
}
