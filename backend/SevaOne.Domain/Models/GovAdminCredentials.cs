namespace SevaOne.Domain.Models;

/// <summary>Gov-admin login lookup only -- mirrors OperatorCredentials, minus a password hash:
/// gov admins have no local password, WSO2 IS is the only credential store (see
/// portal.sp_get_gov_admin_by_username). Username is the permanent Superadmin-chosen identifier,
/// stable from creation; Wso2Subject stays null until GovAdminClaimService's first-time-login
/// claim completes (see V69__gov_admin_master_claim_flow.sql) -- login must check for that before
/// attempting a WSO2 login.</summary>
public sealed class GovAdminCredentials
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Wso2Subject { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string? AssignedDistrict { get; set; }
    public string? AssignedTaluk { get; set; }
    public string? AssignedDepartment { get; set; }
    public bool IsActive { get; set; }
}
