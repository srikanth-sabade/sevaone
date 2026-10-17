using SevaOne.Domain.Enums;

namespace SevaOne.Business.Common;

/// <summary>Populated per-request by Services-layer middleware from the seva.govadmin.sid
/// cookie -- the gov-admin-session counterpart of ICurrentOperator. Scoped, not singleton.
/// GovDashboardService reads Tier/AssignedDistrict/AssignedTaluk from here (and only from here)
/// to resolve analytics scoping -- never from a client-supplied query parameter.</summary>
public interface ICurrentGovAdmin
{
    bool IsAuthenticated { get; }
    Guid? GovAdminId { get; }
    string? Wso2Subject { get; }
    string? DisplayName { get; }
    GovAdminTier? Tier { get; }
    string? AssignedDistrict { get; }
    string? AssignedTaluk { get; }
    string? AssignedDepartment { get; }
    string? SessionId { get; }
    string? IpAddress { get; }
}
