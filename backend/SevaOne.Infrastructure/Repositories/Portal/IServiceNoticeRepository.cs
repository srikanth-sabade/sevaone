using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Portal;

public interface IServiceNoticeRepository
{
    Task<ServiceNotice?> GetAsync(bool activeOnly, CancellationToken ct);
    Task<IReadOnlyList<ServiceNoticeHistory>> ListHistoryAsync(CancellationToken ct);
    Task<ServiceNotice> UpsertAsync(ServiceNotice notice, Guid? updatedBy, CancellationToken ct);
    Task StopAsync(Guid? stoppedBy, CancellationToken ct);
}
