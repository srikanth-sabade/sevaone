using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

public interface ICatalogManagementService
{
    Task<IReadOnlyList<CatalogAdminDepartmentDto>> ListDepartmentsAsync(CancellationToken ct);
    Task<CatalogAdminDepartmentDto> CreateDepartmentAsync(CatalogAdminDepartmentWriteRequest request, CancellationToken ct);
    Task<CatalogAdminDepartmentDto> UpdateDepartmentAsync(string code, CatalogAdminDepartmentWriteRequest request, CancellationToken ct);
    Task SetDepartmentActiveAsync(string code, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<CatalogAdminSubDepartmentDto>> ListSubDepartmentsAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct);
    Task<CatalogAdminSubDepartmentDto> CreateSubDepartmentAsync(CatalogAdminSubDepartmentWriteRequest request, CancellationToken ct);
    Task<CatalogAdminSubDepartmentDto> UpdateSubDepartmentAsync(Guid id, CatalogAdminSubDepartmentWriteRequest request, CancellationToken ct);
    Task SetSubDepartmentActiveAsync(Guid id, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<CatalogAdminServiceListItemDto>> ListServicesAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct);
    Task<CatalogAdminServiceDto> GetServiceAsync(string departmentCode, string serviceCode, CancellationToken ct);
    Task<CatalogAdminServiceDto> CreateServiceAsync(CatalogAdminServiceWriteRequest request, CancellationToken ct);
    Task<CatalogAdminServiceDto> UpdateServiceAsync(string departmentCode, string serviceCode, CatalogAdminServiceWriteRequest request, CancellationToken ct);
    Task SetServiceActiveAsync(string departmentCode, string serviceCode, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<CatalogAdminChangeDto>> ListChangesAsync(int limit, CancellationToken ct);
}
