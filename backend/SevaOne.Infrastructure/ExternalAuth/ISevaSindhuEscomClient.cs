namespace SevaOne.Infrastructure.ExternalAuth;

public interface ISevaSindhuEscomClient
{
    /// <summary>
    /// Calls Seva Sindhu's ESCOM token_generation API for the given service and returns the raw
    /// Token. Throws <see cref="SevaOne.Domain.Common.DomainException"/> with a citizen-safe
    /// message on any failure -- network error, timeout, non-2xx, malformed JSON, a
    /// non-"200" statusCode, or a missing Token -- never the raw government API error text.
    /// The token itself is never logged.
    /// </summary>
    Task<string> GenerateTokenAsync(string serviceId, string userId, CancellationToken ct);
}
