namespace SevaOne.Infrastructure.Caching;

/// <summary>The gov-admin panel's cookie session, cached in Redis -- mirrors OperatorSession/
/// AdminSession. Separate cookie, separate auth scheme -- a gov admin, an admin, an operator,
/// and a citizen can all be logged in on the same browser at once.
///
/// Tier/AssignedDistrict/AssignedTaluk are baked in here at login time (from
/// portal.gov_admin_master, via GovAdminAuthService) rather than re-read from Postgres on every
/// request -- GovAdminSessionMiddleware just reads them off the cached session, same per-request
/// cost as the operator/admin sessions.</summary>
public sealed class GovAdminSession
{
    public string SessionId { get; set; } = string.Empty;
    public Guid GovAdminId { get; set; }
    public string Wso2Subject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string? AssignedDistrict { get; set; }
    public string? AssignedTaluk { get; set; }
    public string? AssignedDepartment { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
