using SevaOne.Business.Discovery.DTOs;
namespace SevaOne.Business.Discovery.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(CancellationToken ct);
    Task<IReadOnlyList<SchemeSummaryDto>> ListSchemesAsync(ListSchemesRequest request, CancellationToken ct);
    Task<SchemeDetailDto> GetSchemeAsync(GetSchemeRequest request, CancellationToken ct);
    Task<ServiceSearchResultDto> SearchServicesAsync(SearchServicesRequest request, CancellationToken ct);

    /// <summary>Backs the landing page's "Frequently requested" section -- real apply counts,
    /// not an arbitrary slice of the catalog. See ListTopRequestedServicesAsync for the ranking
    /// rule (real applies first, zero-apply services pad the rest so this never returns fewer
    /// than `limit`).</summary>
    Task<IReadOnlyList<SchemeSummaryDto>> ListPopularSchemesAsync(int limit, CancellationToken ct);
}
