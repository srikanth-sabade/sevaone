using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Infrastructure.Repositories.GovAdmin;

namespace SevaOne.Business.Portal.Services;

/// <summary>
/// Read-only gov-admin analytics. <see cref="ResolveScope"/> is the ONE place that turns a
/// caller's tier into a (district, taluk) filter -- Superadmin/ChiefMinister/Department get
/// (null, null) i.e. no district/taluk filter, DistrictHead gets (AssignedDistrict, null),
/// TalukHead gets (AssignedDistrict, AssignedTaluk). <see cref="ResolveDepartmentScope"/> is the
/// equivalent for the Department tier's own scope. This must never be bypassed: every repository
/// call below passes only what these methods return, never a value from the request. The SQL
/// functions themselves trust whatever they're given -- this is the actual RBAC enforcement
/// point for scoped data.
/// </summary>
public sealed class GovDashboardService : IGovDashboardService
{
    private readonly IGovDashboardRepository _repo;
    private readonly ICurrentGovAdmin _current;

    public GovDashboardService(IGovDashboardRepository repo, ICurrentGovAdmin current)
    {
        _repo = repo;
        _current = current;
    }

    public async Task<GovOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope();
        // Citizens have no department relationship at all -- a Department-tier admin's overview
        // omits citizen figures entirely rather than showing a misleading statewide number
        // alongside their department-scoped application/discovery figures.
        var totals = department is null ? await _repo.GetCitizenTotalsAsync(district, taluk, ct) : null;
        var statusRows = await _repo.GetApplicationStatusBreakdownAsync(district, taluk, department, ct);
        var sourceRows = await _repo.GetApplySourceSplitAsync(district, taluk, department, ct);

        return new GovOverviewDto(
            totals?.RegisteredCitizens ?? 0,
            totals?.ProfileCompletedCitizens ?? 0,
            BucketStatuses(statusRows),
            sourceRows.Select(r => new ApplySourceCountDto(r.Source, r.ClickCount)).ToList());
    }

    public async Task<GovDepartmentsDto> GetDepartmentsAsync(CancellationToken ct)
    {
        if (_current.Tier is GovAdminTier.DistrictHead or GovAdminTier.TalukHead)
        {
            // Department/service counts aren't meaningfully district-scoped -- statewide tiers
            // only, enforced here rather than relying on the frontend to simply not show the page.
            throw new ForbiddenException("Department analytics are not available for this role.");
        }

        var counts = await _repo.GetDepartmentServiceCountsAsync(ct);
        var byDepartment = await _repo.GetDepartmentBreakdownAsync(ct);

        return new GovDepartmentsDto(
            counts.DepartmentCount,
            counts.ActiveServiceCount,
            byDepartment.Select(r => new DepartmentServiceCountDto(r.DepartmentCode, r.DepartmentName, r.ServiceCount)).ToList());
    }

    public async Task<GovDistrictsDto> GetDistrictsAsync(CancellationToken ct)
    {
        if (_current.Tier is GovAdminTier.Department)
        {
            // Districts have no department relationship -- excluded for the same reason as
            // Overview's citizen figures above.
            throw new ForbiddenException("District analytics are not available for this role.");
        }

        var (district, taluk) = ResolveScope();
        var rows = await _repo.GetDistrictRowsAsync(district, taluk, ct);

        return new GovDistrictsDto(
            ScopeName(),
            rows.Select(r => new DistrictRowDto(
                r.District, r.Taluk, r.ApplicationCount, r.CitizenCount,
                r.SubmittedCount, r.InProgressCount, r.CompletedCount, r.RejectedCount)).ToList());
    }

    public async Task<GovDiscoveryDto> GetDiscoveryAsync(CancellationToken ct)
    {
        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope();
        var totals = await _repo.GetDiscoveryTotalsAsync(district, taluk, department, ct);
        var byDepartment = await _repo.GetDiscoveryByDepartmentAsync(district, taluk, department, ct);

        return new GovDiscoveryDto(
            totals.TotalApplyClicks,
            totals.AiApplyClicks,
            totals.CatalogueApplyClicks,
            byDepartment.Select(r => new DepartmentDiscoveryCountDto(r.DepartmentCode, r.AiCount, r.CatalogueCount)).ToList());
    }

    public async Task<GovCitizensDto> GetCitizensAsync(CancellationToken ct)
    {
        if (_current.Tier is GovAdminTier.Department)
        {
            throw new ForbiddenException("Citizen analytics are not available for this role.");
        }

        var (district, taluk) = ResolveScope();
        var totals = await _repo.GetCitizenTotalsAsync(district, taluk, ct);
        var trend = await _repo.GetRegistrationTrendAsync(district, taluk, ct);

        return new GovCitizensDto(
            totals.RegisteredCitizens,
            totals.ProfileCompletedCitizens,
            totals.RegisteredCitizens - totals.ProfileCompletedCitizens,
            trend.Select(r => new RegistrationTrendPointDto(r.RegDate, r.Registrations)).ToList());
    }

    public async Task<GovLifecycleDto> GetLifecycleAsync(CancellationToken ct)
    {
        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope();
        var statusRows = await _repo.GetApplicationStatusBreakdownAsync(district, taluk, department, ct);
        var trend = await _repo.GetLifecycleTrendAsync(district, taluk, department, ct);

        return new GovLifecycleDto(
            BucketStatuses(statusRows),
            trend.Select(r => new LifecycleTrendPointDto(r.AppDate, r.Submitted, r.Approved, r.Rejected)).ToList());
    }

    public async Task<GovSakalaSlaDto> GetSakalaSlaAsync(CancellationToken ct)
    {
        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope();
        var rows = await _repo.GetSakalaSlaAsync(district, taluk, department, ct);

        return new GovSakalaSlaDto(
            ScopeName(),
            rows.Select(r => new SakalaSlaDepartmentRowDto(
                r.DepartmentCode, r.DepartmentName, r.TotalSakalaApplications,
                r.CompletedOnTime, r.CompletedLate, r.PendingWithinSla, r.PendingOverdue)).ToList());
    }

    public async Task<GovApplicationAgingDto> GetApplicationAgingAsync(CancellationToken ct)
    {
        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope();
        var rows = await _repo.GetApplicationAgingAsync(district, taluk, department, ct);

        return new GovApplicationAgingDto(
            ScopeName(),
            rows.Select(r => new ApplicationAgingRowDto(
                r.DepartmentCode, r.DepartmentName, r.OpenApplications, r.DueWithin24h,
                r.Age0To3Days, r.Age4To7Days, r.Age8To14Days, r.Age15To30Days, r.Age30PlusDays,
                r.AvgProcessingHours)).ToList());
    }

    public async Task<GovSlaTrendDto> GetSlaTrendAsync(CancellationToken ct)
    {
        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope();
        var trend = await _repo.GetSlaTrendAsync(district, taluk, department, ct);

        return new GovSlaTrendDto(
            ScopeName(),
            trend.Select(r => new SlaTrendPointDto(r.PeriodStart, r.Total, r.CompletedOnTime, r.CompletedLate)).ToList());
    }

    public async Task<GovServiceUsageDto> GetServiceUsageAsync(DateOnly? since, DateOnly? until, string? departmentCode, CancellationToken ct)
    {
        if (since.HasValue && until.HasValue && until.Value < since.Value)
        {
            throw new DomainException("The end date must be on or after the start date.");
        }

        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope() ?? (string.IsNullOrWhiteSpace(departmentCode) ? null : departmentCode.Trim());
        var sinceAt = since?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var untilAt = until?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rows = await _repo.GetServiceUsageAsync(district, taluk, department, sinceAt, untilAt, ct);
        return new GovServiceUsageDto(ScopeName(), rows.Select(row => new ServiceUsageRowDto(
            row.ServiceCode, row.ServiceName, row.DepartmentCode, row.DepartmentName,
            row.TotalApplyClicks, row.AiApplyClicks, row.CatalogueApplyClicks, row.LastUsedAt)).ToList());
    }

    public async Task<GovApplicationFunnelDto> GetApplicationFunnelAsync(CancellationToken ct)
    {
        if (_current.Tier is GovAdminTier.Department)
        {
            throw new ForbiddenException("Application funnel analytics are not available for this role.");
        }

        var (district, taluk) = ResolveScope();
        var row = await _repo.GetApplicationFunnelAsync(district, taluk, ct);
        return new GovApplicationFunnelDto(ScopeName(), row.RegisteredCitizens, row.ProfileCompletedCitizens, row.CitizensSubmitted, row.CitizensCompleted);
    }

    public async Task<GovApplicationDropoffDto> GetApplicationDropoffAsync(CancellationToken ct)
    {
        var (district, taluk) = ResolveScope();
        var department = ResolveDepartmentScope();
        var rows = await _repo.GetApplicationDropoffAsync(district, taluk, department, ct);
        return new GovApplicationDropoffDto(ScopeName(), rows.Select(row => new ApplicationDropoffRowDto(
            ((ApplicationStatus)row.Status).ToString(), row.OpenCount, row.AvgTimeInStatusHours)).ToList());
    }

    private (string? District, string? Taluk) ResolveScope() => _current.Tier switch
    {
        GovAdminTier.DistrictHead => (_current.AssignedDistrict, null),
        GovAdminTier.TalukHead => (_current.AssignedDistrict, _current.AssignedTaluk),
        _ => (null, null)
    };

    private string? ResolveDepartmentScope() => _current.Tier == GovAdminTier.Department ? _current.AssignedDepartment : null;

    private string ScopeName() => _current.Tier switch
    {
        GovAdminTier.DistrictHead => "district",
        GovAdminTier.TalukHead => "taluk",
        _ => "statewide"
    };

    // Buckets ApplicationStatus's 5 internal workflow stages into the 3 outcome-level labels the
    // admin-panel frontend renders (Overview's status chart, Lifecycle's Completed/Pending/
    // Rejected cards -- see admin-panel/src/pages/Lifecycle.tsx's exact-name lookup by
    // `get('Completed')`/`get('Pending')`/`get('Rejected')`): a gov admin cares about outcomes,
    // not internal stage names, and the frontend was built against exactly these 3 labels. Order
    // is fixed (not alphabetical/enum order) so a caller that indexes by position stays stable.
    private static IReadOnlyList<StatusCountDto> BucketStatuses(IReadOnlyList<GovStatusCountRow> rows)
    {
        long completed = 0, pending = 0, rejected = 0;
        foreach (var row in rows)
        {
            switch ((ApplicationStatus)row.Status)
            {
                case ApplicationStatus.Approved:
                    completed += row.StatusCount;
                    break;
                case ApplicationStatus.Rejected:
                    rejected += row.StatusCount;
                    break;
                default:
                    pending += row.StatusCount;
                    break;
            }
        }

        return new List<StatusCountDto>
        {
            new("Completed", completed),
            new("Pending", pending),
            new("Rejected", rejected)
        };
    }
}
