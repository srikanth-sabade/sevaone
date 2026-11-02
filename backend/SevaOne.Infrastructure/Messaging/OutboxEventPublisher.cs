using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;
using SevaOne.Infrastructure.Repositories.Orchestration;

namespace SevaOne.Infrastructure.Messaging;

public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly IOrchestrationRepository _repository;

    public OutboxEventPublisher(IOrchestrationRepository repository)
    {
        _repository = repository;
    }

    public Task EnqueueAsync(string topic, string payload, IUnitOfWork? uow, CancellationToken ct)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = topic,
            Payload = payload,
            CreatedAt = DateTimeOffset.UtcNow,
            Attempts = 0
        };
        return _repository.InsertOutboxMessageAsync(message, uow, ct);
    }
}
