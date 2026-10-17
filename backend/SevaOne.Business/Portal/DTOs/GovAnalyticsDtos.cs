namespace SevaOne.Business.Portal.DTOs;

// Response shapes for GET /api/v1/admin/gov/analytics/* -- these are the frozen contract in
// SESSION_HANDOFF.md's "Government Admin Panel" section, shared with the admin-panel frontend
// (Track B). Change the shape here only alongside updating that doc, since the frontend builds
// against it independently. All aggregate-only -- no PII field belongs on any of these.

public sealed record StatusCountDto(string Status, long Count);
public sealed record ApplySourceCountDto(string Source, long Count);
public sealed record DepartmentServiceCountDto(string DepartmentCode, string DepartmentName, long ServiceCount);
public sealed record DistrictRowDto(
    string District,
    string? Taluk,
    long ApplicationCount,
    long CitizenCount,
    long SubmittedCount,
    long InProgressCount,
    long CompletedCount,
    long RejectedCount);
public sealed record DepartmentDiscoveryCountDto(string DepartmentCode, long AiCount, long CatalogueCount);
public sealed record RegistrationTrendPointDto(DateOnly Date, long Count);
public sealed record LifecycleTrendPointDto(DateOnly Date, long Submitted, long Approved, long Rejected);
public sealed record SakalaSlaDepartmentRowDto(
    string DepartmentCode,
    string DepartmentName,
    long TotalSakalaApplications,
    long CompletedOnTime,
    long CompletedLate,
    long PendingWithinSla,
    long PendingOverdue);
public sealed record ApplicationAgingRowDto(
    string DepartmentCode,
    string DepartmentName,
    long OpenApplications,
    long DueWithin24h,
    long Age0To3Days,
    long Age4To7Days,
    long Age8To14Days,
    long Age15To30Days,
    long Age30PlusDays,
    double? AvgProcessingHours);
public sealed record SlaTrendPointDto(DateOnly PeriodStart, long Total, long CompletedOnTime, long CompletedLate);

public sealed record GovOverviewDto(
    long RegisteredCitizens,
    long ProfileCompletedCitizens,
    IReadOnlyList<StatusCountDto> ApplicationsByStatus,
    IReadOnlyList<ApplySourceCountDto> ApplySource);

public sealed record GovDepartmentsDto(
    long DepartmentCount,
    long ActiveServiceCount,
    IReadOnlyList<DepartmentServiceCountDto> ByDepartment);

public sealed record GovDistrictsDto(
    string Scope,
    IReadOnlyList<DistrictRowDto> Rows);

public sealed record GovDiscoveryDto(
    long TotalApplyClicks,
    long AiApplyClicks,
    long CatalogueApplyClicks,
    IReadOnlyList<DepartmentDiscoveryCountDto> ByDepartment);

public sealed record GovCitizensDto(
    long TotalRegistered,
    long ProfileCompleted,
    long ProfileIncomplete,
    IReadOnlyList<RegistrationTrendPointDto> RegistrationTrend);

public sealed record GovLifecycleDto(
    IReadOnlyList<StatusCountDto> ByStatus,
    IReadOnlyList<LifecycleTrendPointDto> Trend);

public sealed record GovSakalaSlaDto(
    string Scope,
    IReadOnlyList<SakalaSlaDepartmentRowDto> ByDepartment);

public sealed record GovApplicationAgingDto(
    string Scope,
    IReadOnlyList<ApplicationAgingRowDto> ByDepartment);

public sealed record GovSlaTrendDto(
    string Scope,
    IReadOnlyList<SlaTrendPointDto> Trend);

public sealed record ServiceUsageRowDto(
    string ServiceCode, string ServiceName, string DepartmentCode, string DepartmentName,
    long TotalApplyClicks, long AiApplyClicks, long CatalogueApplyClicks, DateTimeOffset? LastUsedAt);

public sealed record GovServiceUsageDto(string Scope, IReadOnlyList<ServiceUsageRowDto> Services);

public sealed record GovApplicationFunnelDto(
    string Scope, long RegisteredCitizens, long ProfileCompletedCitizens,
    long CitizensSubmitted, long CitizensCompleted);

public sealed record ApplicationDropoffRowDto(string Status, long OpenCount, decimal AvgTimeInStatusHours);

public sealed record GovApplicationDropoffDto(string Scope, IReadOnlyList<ApplicationDropoffRowDto> ByStatus);
