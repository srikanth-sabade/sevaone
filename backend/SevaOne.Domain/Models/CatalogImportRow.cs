using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// discovery.catalog_import_row. One row per parsed service within a batch. Staging only --
/// never read by the live citizen/operator catalog, purely the admin review surface before
/// publish. Written in two passes: inserted with RawPayload/NormalizedPayload, then updated
/// with Action/errors/warnings once cross-row checks (e.g. a duplicate composite code within
/// the batch) have run over the whole batch.
/// </summary>
public sealed class CatalogImportRow : BaseModel
{
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }

    /// <summary>JSON, exactly as parsed from the source.</summary>
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>JSON, after normalization rules run.</summary>
    public string? NormalizedPayload { get; set; }

    /// <summary>"insert" / "update" / "unchanged" / "conflict" -- null until validated.</summary>
    public string? Action { get; set; }

    /// <summary>JSON array of {field, message}.</summary>
    public string ValidationErrors { get; set; } = "[]";

    /// <summary>JSON array of {field, message}.</summary>
    public string ValidationWarnings { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }
}
