using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// discovery.service_change_log. Append-only audit trail -- who changed what on which
/// service, when, and (if it came from an upload) which batch. Independent of
/// <see cref="CatalogImportRow"/>: manual single-service edits and visibility grants/revokes
/// log here too, not just imports.
/// </summary>
public sealed class ServiceChangeLog : BaseModel
{
    public Guid? ServiceId { get; set; }
    public Guid? BatchId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>"created" / "updated" / "deactivated" / "reactivated" / "role_grant" / "role_revoke" -- CHECK-constrained at the DB.</summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>JSON, {field: {old, new}, ...}. Null for events like role_grant/role_revoke that aren't a field diff.</summary>
    public string? Diff { get; set; }
}
