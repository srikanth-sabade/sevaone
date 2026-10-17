using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Infrastructure.Repositories.GovAdmin;

namespace SevaOne.Business.Portal.Services;

/// <summary>Same viewer tier as the admin accounts list (GovAdminManagementService.ListAsync):
/// Superadmin and ChiefMinister can see who did what, neither tier can act on it from here.</summary>
public sealed class GovAdminActivityLogService : IGovAdminActivityLogService
{
    private readonly IGovAdminActivityLogRepository _repository;
    private readonly ICurrentGovAdmin _current;

    public GovAdminActivityLogService(IGovAdminActivityLogRepository repository, ICurrentGovAdmin current)
    {
        _repository = repository;
        _current = current;
    }

    public async Task<IReadOnlyList<GovAdminActivityLogDto>> ListAsync(int limit, string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (_current.Tier != GovAdminTier.Superadmin && _current.Tier != GovAdminTier.ChiefMinister)
        {
            throw new ForbiddenException("You don't have access to: viewing the activity log.");
        }

        var safeLimit = Math.Clamp(limit, 1, 500);
        var rows = await _repository.ListAsync(safeLimit, action, from, to, ct);
        return rows.Select(r => new GovAdminActivityLogDto(r.Id, r.ActorUsername, r.ActorDisplayName, r.Action, r.Target, r.Detail, r.At)).ToList();
    }
}
