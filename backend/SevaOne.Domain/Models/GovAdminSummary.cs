namespace SevaOne.Domain.Models;

/// <summary>One row of the Superadmin/ChiefMinister admin-management list -- no password/WSO2
/// token data, just what's safe to show in a management UI. Claimed is derived from
/// wso2_subject's presence (see V69__gov_admin_master_claim_flow.sql).</summary>
public sealed class GovAdminSummary
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string? AssignedDistrict { get; set; }
    public string? AssignedTaluk { get; set; }
    public string? AssignedDepartment { get; set; }
    public bool IsActive { get; set; }
    public bool Claimed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
