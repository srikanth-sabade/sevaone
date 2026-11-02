using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>Role-filtered reads backing the redesigned /catalog page. "role" is always resolved by the caller from the request's identity (CatalogController), never accepted as a client-supplied parameter -- a citizen session must not be able to ask for operator-only data by passing a different role string.</summary>
public interface ICatalogHierarchyService
{
    Task<IReadOnlyList<CatalogHierarchyDepartmentDto>> ListDepartmentsAsync(string role, CancellationToken ct);

    /// <summary>Null if the department doesn't exist -- the caller 404s either way, same as a role-excluded service (see GetServiceAsync).</summary>
    Task<CatalogDepartmentDetailDto?> GetDepartmentAsync(string departmentCode, string role, CancellationToken ct);

    /// <summary>Null if the service doesn't exist OR isn't visible to this role -- indistinguishable on purpose, so an excluded service's existence never leaks.</summary>
    Task<CatalogHierarchyServiceDto?> GetServiceAsync(string departmentCode, string serviceCode, string role, CancellationToken ct);

    /// <summary>Landing-page search: departments matching by name (one card, not every service under it as rows) plus services matching by their own name/code or their sub-department's name.</summary>
    Task<CatalogSearchResultDto> SearchAsync(string query, string role, CancellationToken ct);

    /// <summary>Every citizen-visible service across every department, for the AI backend's
    /// catalog ingestion. No role parameter, unlike the other reads here -- this is a
    /// server-to-server export with no request identity behind it, and always means the
    /// citizen-visible set specifically, since it only ever feeds the citizen-facing AI
    /// assistant.</summary>
    Task<IReadOnlyList<AiCatalogExportServiceDto>> ExportForAiAsync(CancellationToken ct);
}
