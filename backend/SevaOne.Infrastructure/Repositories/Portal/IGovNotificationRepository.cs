using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Portal;

public interface IGovNotificationRepository
{
    Task<IReadOnlyList<GovNotification>> ListAsync(bool activeOnly, CancellationToken ct);
    Task<GovNotification> CreateAsync(GovNotification notification, CancellationToken ct);
    Task SetActiveAsync(Guid id, bool isActive, Guid? updatedBy, CancellationToken ct);
}
