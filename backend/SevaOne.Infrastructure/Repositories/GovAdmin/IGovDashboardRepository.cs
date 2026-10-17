namespace SevaOne.Infrastructure.Repositories.GovAdmin;

/// <summary>Read-only analytics for the gov-admin panel -- kept separate from IPortalRepository
/// since these are reporting queries spanning portal/discovery/orchestration, not portal-schema
/// writes. Every method's district/taluk params: NULL = no filter (statewide), a value scopes
/// to it -- callers (GovDashboardService) decide which to pass; these methods trust whatever
/// they're given.</summary>
public interface IGovDashboardRepository
{
    Task<GovCitizenTotalsRow> GetCitizenTotalsAsync(string? district, string? taluk, CancellationToken ct);
    Task<IReadOnlyList<GovRegistrationTrendRow>> GetRegistrationTrendAsync(string? district, string? taluk, CancellationToken ct);
    Task<IReadOnlyList<GovStatusCountRow>> GetApplicationStatusBreakdownAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<IReadOnlyList<GovApplySourceRow>> GetApplySourceSplitAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<GovDepartmentServiceCountsRow> GetDepartmentServiceCountsAsync(CancellationToken ct);
    Task<IReadOnlyList<GovDepartmentBreakdownRow>> GetDepartmentBreakdownAsync(CancellationToken ct);
    Task<IReadOnlyList<GovDistrictRow>> GetDistrictRowsAsync(string? district, string? taluk, CancellationToken ct);
    Task<GovDiscoveryTotalsRow> GetDiscoveryTotalsAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<IReadOnlyList<GovDiscoveryByDepartmentRow>> GetDiscoveryByDepartmentAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<IReadOnlyList<GovLifecycleTrendRow>> GetLifecycleTrendAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<IReadOnlyList<GovSakalaSlaRow>> GetSakalaSlaAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<IReadOnlyList<GovApplicationAgingRow>> GetApplicationAgingAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<IReadOnlyList<GovSlaTrendRow>> GetSlaTrendAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
    Task<IReadOnlyList<GovServiceUsageRow>> GetServiceUsageAsync(string? district, string? taluk, string? departmentCode, DateTimeOffset? since, DateTimeOffset? until, CancellationToken ct);
    Task<GovApplicationFunnelRow> GetApplicationFunnelAsync(string? district, string? taluk, CancellationToken ct);
    Task<IReadOnlyList<GovApplicationDropoffRow>> GetApplicationDropoffAsync(string? district, string? taluk, string? departmentCode, CancellationToken ct);
}
