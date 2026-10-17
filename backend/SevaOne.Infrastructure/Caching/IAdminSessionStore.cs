namespace SevaOne.Infrastructure.Caching;

public interface IAdminSessionStore
{
    Task SaveAsync(AdminSession session, TimeSpan ttl, CancellationToken ct);
    Task<AdminSession?> GetAsync(string sessionId, CancellationToken ct);
    Task DeleteAsync(string sessionId, CancellationToken ct);
}
