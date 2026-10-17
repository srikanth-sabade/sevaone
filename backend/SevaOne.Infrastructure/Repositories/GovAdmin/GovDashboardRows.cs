namespace SevaOne.Infrastructure.Repositories.GovAdmin;

// Dapper row shapes for the R__sp_get_gov_*.sql analytics functions -- kept separate from the
// Business-layer GovAnalyticsDtos.cs response DTOs (GovDashboardService composes one from the
// other) so a SQL column rename doesn't ripple straight into the API contract.

public sealed class GovCitizenTotalsRow
{
    public long RegisteredCitizens { get; set; }
    public long ProfileCompletedCitizens { get; set; }
}

public sealed class GovStatusCountRow
{
    public int Status { get; set; }
    public long StatusCount { get; set; }
}

public sealed class GovApplySourceRow
{
    public string Source { get; set; } = string.Empty;
    public long ClickCount { get; set; }
}

public sealed class GovDepartmentServiceCountsRow
{
    public long DepartmentCount { get; set; }
    public long ActiveServiceCount { get; set; }
}

public sealed class GovDepartmentBreakdownRow
{
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public long ServiceCount { get; set; }
}

public sealed class GovDistrictRow
{
    public string District { get; set; } = string.Empty;
    public string? Taluk { get; set; }
    public long ApplicationCount { get; set; }
    public long CitizenCount { get; set; }
    public long SubmittedCount { get; set; }
    public long InProgressCount { get; set; }
    public long CompletedCount { get; set; }
    public long RejectedCount { get; set; }
}

public sealed class GovDiscoveryTotalsRow
{
    public long TotalApplyClicks { get; set; }
    public long AiApplyClicks { get; set; }
    public long CatalogueApplyClicks { get; set; }
}

public sealed class GovDiscoveryByDepartmentRow
{
    public string DepartmentCode { get; set; } = string.Empty;
    public long AiCount { get; set; }
    public long CatalogueCount { get; set; }
}

public sealed class GovRegistrationTrendRow
{
    public DateOnly RegDate { get; set; }
    public long Registrations { get; set; }
}

public sealed class GovLifecycleTrendRow
{
    public DateOnly AppDate { get; set; }
    public long Submitted { get; set; }
    public long Approved { get; set; }
    public long Rejected { get; set; }
}

public sealed class GovSakalaSlaRow
{
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public long TotalSakalaApplications { get; set; }
    public long CompletedOnTime { get; set; }
    public long CompletedLate { get; set; }
    public long PendingWithinSla { get; set; }
    public long PendingOverdue { get; set; }
}

public sealed class GovApplicationAgingRow
{
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public long OpenApplications { get; set; }
    public long DueWithin24h { get; set; }
    public long Age0To3Days { get; set; }
    public long Age4To7Days { get; set; }
    public long Age8To14Days { get; set; }
    public long Age15To30Days { get; set; }
    public long Age30PlusDays { get; set; }
    public double? AvgProcessingHours { get; set; }
}

public sealed class GovSlaTrendRow
{
    public DateOnly PeriodStart { get; set; }
    public long Total { get; set; }
    public long CompletedOnTime { get; set; }
    public long CompletedLate { get; set; }
}

public sealed class GovServiceUsageRow
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public long TotalApplyClicks { get; set; }
    public long AiApplyClicks { get; set; }
    public long CatalogueApplyClicks { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}

public sealed class GovApplicationFunnelRow
{
    public long RegisteredCitizens { get; set; }
    public long ProfileCompletedCitizens { get; set; }
    public long CitizensSubmitted { get; set; }
    public long CitizensCompleted { get; set; }
}

public sealed class GovApplicationDropoffRow
{
    public int Status { get; set; }
    public long OpenCount { get; set; }
    public decimal AvgTimeInStatusHours { get; set; }
}
