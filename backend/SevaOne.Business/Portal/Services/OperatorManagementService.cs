using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Infrastructure.Repositories.Portal;

namespace SevaOne.Business.Portal.Services;

public sealed class OperatorManagementService : IOperatorManagementService
{
    private readonly IPortalRepository _portal;
    private readonly ICurrentGovAdmin _current;

    public OperatorManagementService(IPortalRepository portal, ICurrentGovAdmin current)
    {
        _portal = portal;
        _current = current;
    }

    public async Task<IReadOnlyList<OperatorAdminListItem>> ListAsync(string? search, bool? isActive, string? operatorType, CancellationToken ct)
    {
        RequireViewAccess();
        var rows = await _portal.ListOperatorsForAdminAsync(search, isActive, operatorType, ct);
        return rows.Select(row => new OperatorAdminListItem(
            row.Id, row.OperatorCode, row.OperatorType, row.OperatorName, row.MobileNo,
            row.IsClaimed, row.IsActive, row.CreatedAt)).ToList();
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        RequireSuperadmin("activating or deactivating an operator");
        if (!await _portal.SetOperatorActiveAsync(id, isActive, ct))
        {
            throw new NotFoundException("Operator not found.");
        }
    }

    private void RequireViewAccess()
    {
        if (_current.Tier is not (GovAdminTier.Superadmin or GovAdminTier.ChiefMinister))
        {
            throw new ForbiddenException("You don't have access to the operator directory.");
        }
    }

    private void RequireSuperadmin(string action)
    {
        if (_current.Tier != GovAdminTier.Superadmin)
        {
            throw new ForbiddenException($"Only a Superadmin can perform this action: {action}.");
        }
    }
}
