using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Notification;

public interface INotificationRepository
{
    Task<NotificationMessage> InsertAsync(NotificationMessage message, CancellationToken ct);
    Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken ct);
    Task<IReadOnlyList<NotificationMessage>> GetPendingAsync(int batchSize, CancellationToken ct);
}
