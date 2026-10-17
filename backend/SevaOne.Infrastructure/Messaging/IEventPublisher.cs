using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Messaging;

/// <summary>
/// Business code never talks to Kafka directly — it enqueues into orchestration.outbox_messages
/// in the same transaction as the business write, so the message can never be lost to a crash
/// between "wrote the row" and "published the event". OutboxDispatcher (a hosted service) drains
/// the table and does the actual Kafka send.
/// </summary>
public interface IEventPublisher
{
    /// <summary>Kafka message key is the outbox row's own id, assigned at publish time — see OutboxDispatcher.</summary>
    Task EnqueueAsync(string topic, string payload, IUnitOfWork? uow, CancellationToken ct);
}
