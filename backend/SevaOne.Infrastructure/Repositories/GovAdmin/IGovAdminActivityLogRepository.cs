using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.GovAdmin;

public interface IGovAdminActivityLogRepository
{
    Task InsertAsync(GovAdminActivityLog log, CancellationToken ct);

    Task<IReadOnlyList<GovAdminActivityLog>> ListAsync(int limit, string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}
