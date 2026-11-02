using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>The citizen apply click for a service whose department checks tokens against Seva Sindhu, but whose entry
/// page expects us to POST the token to it ourselves (the original method), rather than Seva Sindhu's own redirect
/// endpoint sending the citizen there. Same Seva Sindhu generate call as "edcs_api"; the department URL is ours to
/// hold and post to, so it needs to be set on the integration.</summary>
public interface IEdcsApiFormTokenService
{
    Task<ApplyOutcomeDto> StartAsync(Citizen citizen, Scheme scheme, Department department, TokenIntegrationActiveRow integration, CancellationToken ct, IReadOnlyDictionary<string, string>? answers = null);
}
