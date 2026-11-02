namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>For applications started with a token from Seva Sindhu's API: asks that API whether the department has reported
/// an acknowledgement yet and, when it has, records it the same way a direct acknowledgement is recorded.</summary>
public interface IEdcsApiAckService
{
    /// <summary>Looks at up to <paramref name="limit"/> waiting applications. Returns how many were acknowledged.</summary>
    Task<int> PollDueAsync(int limit, CancellationToken ct);
}
