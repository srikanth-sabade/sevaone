using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Audit;

public interface IAuditRepository
{
    Task InsertAsync(AuditLog log, CancellationToken ct);
    Task<IReadOnlyList<AuditLog>> GetByCitizenAsync(Guid citizenId, int take, CancellationToken ct);
}
