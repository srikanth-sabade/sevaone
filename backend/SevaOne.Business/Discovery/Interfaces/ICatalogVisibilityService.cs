using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

public interface ICatalogVisibilityService
{
    Task<IReadOnlyList<CatalogRoleDto>> ListRolesAsync(CancellationToken ct);

    Task<IReadOnlyList<ServiceVisibilityDto>> GetServiceVisibilityAsync(string departmentCode, string serviceCode, CancellationToken ct);

    Task SetServiceVisibilityAsync(string departmentCode, string serviceCode, string roleKey, bool grant, string changedBy, CancellationToken ct);

    /// <summary>Every service in DepartmentCode (or, if SubDepartmentName is also given, just that sub-department) gets every role in RoleKeys granted or revoked -- the admin console's "apply to all N services in Revenue Department" action.</summary>
    Task<BulkVisibilityResult> BulkSetVisibilityAsync(BulkVisibilityRequest request, string changedBy, CancellationToken ct);

    /// <summary>Both filters optional -- null/null returns the whole catalogue's audit log, most-recent first.</summary>
    Task<IReadOnlyList<ServiceChangeLogDto>> ListChangeLogAsync(string? departmentCode, string? serviceCode, int limit, CancellationToken ct);
}
