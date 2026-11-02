using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>The citizen apply click for a Kaveri 2.0 service: ask Kaveri for a single sign-on address and return it.</summary>
public interface IKaveriSsoService
{
    /// <summary>The address to send the citizen to. Throws a message the citizen can act on when their profile lacks something
    /// Kaveri requires, and a "not available" message when Kaveri cannot be reached or refuses.</summary>
    Task<string> StartAsync(Citizen citizen, Scheme scheme, Department department, TokenIntegrationActiveRow integration, CancellationToken ct);
}
