using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

/// <summary>Read-only gov-admin analytics, scoped by the caller's tier. Each method resolves
/// (district, taluk) from ICurrentGovAdmin internally -- see GovDashboardService, the one place
/// that decides which value to pass to the repository -- never from a caller-supplied
/// parameter, so a client can't widen its own scope.</summary>
public interface IGovDashboardService
{
    Task<GovOverviewDto> GetOverviewAsync(CancellationToken ct);

    /// <summary>Statewide only -- throws ForbiddenException for District Head/Taluk Head, since
    /// department/service counts aren't meaningfully district-scoped.</summary>
    Task<GovDepartmentsDto> GetDepartmentsAsync(CancellationToken ct);

    Task<GovDistrictsDto> GetDistrictsAsync(CancellationToken ct);
    Task<GovDiscoveryDto> GetDiscoveryAsync(CancellationToken ct);
    Task<GovCitizensDto> GetCitizensAsync(CancellationToken ct);
    Task<GovLifecycleDto> GetLifecycleAsync(CancellationToken ct);
    Task<GovSakalaSlaDto> GetSakalaSlaAsync(CancellationToken ct);
    Task<GovApplicationAgingDto> GetApplicationAgingAsync(CancellationToken ct);
    Task<GovSlaTrendDto> GetSlaTrendAsync(CancellationToken ct);
    Task<GovServiceUsageDto> GetServiceUsageAsync(DateOnly? since, DateOnly? until, string? departmentCode, CancellationToken ct);
    Task<GovApplicationFunnelDto> GetApplicationFunnelAsync(CancellationToken ct);
    Task<GovApplicationDropoffDto> GetApplicationDropoffAsync(CancellationToken ct);
}
