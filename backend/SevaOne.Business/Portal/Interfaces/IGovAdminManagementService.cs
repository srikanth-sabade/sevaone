using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

/// <summary>Superadmin's admin-creation/deactivation actions, plus the read-only list
/// ChiefMinister can also see. Every method resolves the caller's own tier from
/// ICurrentGovAdmin and throws ForbiddenException if it isn't allowed to act -- the controller
/// only handles HTTP concerns, this is where the actual permission check lives.</summary>
public interface IGovAdminManagementService
{
    Task<CreateGovAdminResult> CreateAsync(CreateGovAdminRequest request, CancellationToken ct);
    Task<IReadOnlyList<GovAdminListItem>> ListAsync(CancellationToken ct);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<GovAdminSessionDto>> ListSessionsAsync(Guid adminId, CancellationToken ct);
    Task ForceLogoutAsync(Guid adminId, string sessionId, CancellationToken ct);
}
