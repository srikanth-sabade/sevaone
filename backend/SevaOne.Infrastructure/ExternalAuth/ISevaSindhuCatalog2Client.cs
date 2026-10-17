namespace SevaOne.Infrastructure.ExternalAuth;

public interface ISevaSindhuCatalog2Client
{
    /// <summary>
    /// Calls Seva Sindhu's general (non-ESCOM) Sakala token_generation API for the given
    /// service and returns the raw Token. Same failure-handling contract as
    /// ISevaSindhuEscomClient.GenerateTokenAsync -- every failure mode normalizes to one
    /// citizen-safe DomainException, the token is never logged.
    /// </summary>
    Task<string> GenerateTokenAsync(string serviceId, string userId, CancellationToken ct);
}
