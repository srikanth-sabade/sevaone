using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// discovery.catalog_import_batch. One row per catalog file/JSON upload. An upload never
/// writes department_master/sub_department_master/sevaone_service_master directly -- it
/// lands as <see cref="CatalogImportRow"/> rows an admin reviews, and only a publish action
/// (Status -&gt; "published") writes the live tables.
/// </summary>
public sealed class CatalogImportBatch : BaseModel
{
    public string SourceFilename { get; set; } = string.Empty;

    /// <summary>"xlsx" / "csv" / "json" -- CHECK-constrained at the DB.</summary>
    public string SourceFormat { get; set; } = string.Empty;

    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>"validating" / "ready_to_publish" / "published" / "failed" / "discarded" -- CHECK-constrained at the DB.</summary>
    public string Status { get; set; } = "validating";

    public int RowCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
}
