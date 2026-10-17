namespace SevaOne.Infrastructure.Caching;

public interface IOperatorSessionStore
{
    Task SaveAsync(OperatorSession session, TimeSpan ttl, CancellationToken ct);
    Task<OperatorSession?> GetAsync(string sessionId, CancellationToken ct);
    Task DeleteAsync(string sessionId, CancellationToken ct);
}
