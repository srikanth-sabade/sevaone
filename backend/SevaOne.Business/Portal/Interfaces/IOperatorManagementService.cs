using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IOperatorManagementService
{
    Task<IReadOnlyList<OperatorAdminListItem>> ListAsync(string? search, bool? isActive, string? operatorType, CancellationToken ct);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct);
}
