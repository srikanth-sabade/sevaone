using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>discovery.catalog_admin_master. Its own persona -- not necessarily the same people as citizen-service counter operators (portal.operator_master).</summary>
public sealed class CatalogAdmin : BaseModel
{
    public string AdminId { get; set; } = string.Empty;
    public string? AdminName { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
}
