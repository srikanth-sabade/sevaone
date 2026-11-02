namespace SevaOne.Infrastructure.Caching;

public interface IGovAdminSessionStore
{
    Task SaveAsync(GovAdminSession session, TimeSpan ttl, CancellationToken ct);
    Task<GovAdminSession?> GetAsync(string sessionId, CancellationToken ct);
    Task DeleteAsync(string sessionId, CancellationToken ct);
    Task<IReadOnlyList<GovAdminSession>> ListActiveSessionsAsync(Guid? forAdminId, CancellationToken ct);
}
