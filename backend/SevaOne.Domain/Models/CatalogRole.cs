using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// discovery.catalog_role. The set of audiences a catalog service can be made visible to.
/// Deliberately a table, not a fixed enum/CHECK constraint -- adding a new audience later
/// (e.g. a department-specific reviewer role) is an insert, not a migration.
/// </summary>
public sealed class CatalogRole : BaseModel
{
    public string RoleKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
