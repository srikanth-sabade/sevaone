namespace SevaOne.Domain.Enums;

/// <summary>Government admin panel role hierarchy -- see portal.gov_admin_master's tier CHECK
/// constraint (same string values) and GovDashboardService's tier -> (district, taluk) scoping
/// resolution, the one place that decides which of these sees statewide vs. scoped data.</summary>
public enum GovAdminTier
{
    Superadmin,
    ChiefMinister,
    Department,
    DistrictHead,
    TalukHead
}
