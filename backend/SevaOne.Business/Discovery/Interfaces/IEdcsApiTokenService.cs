using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>The citizen apply click for a service whose department checks tokens against Seva Sindhu: ask the Seva Sindhu
/// EP0115 API for the token and application reference, record the handoff, and return the form the browser must POST.</summary>
public interface IEdcsApiTokenService
{
    Task<ApplyOutcomeDto> StartAsync(Citizen citizen, Scheme scheme, Department department, TokenIntegrationActiveRow integration, CancellationToken ct, IReadOnlyDictionary<string, string>? answers = null);
}
