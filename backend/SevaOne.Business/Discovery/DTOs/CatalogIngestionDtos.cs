namespace SevaOne.Business.Discovery.DTOs;

/// <summary>
/// The canonical shape every ingestion path (JSON body today; .xlsx/.csv parsed down to this
/// same shape later) is staged and published from. One object per service — see
/// backend/docs/DATABASE_SCHEMA.md's discovery schema section for where each field lands.
/// </summary>
public sealed class CatalogRow
{
    public CatalogRowDepartment Department { get; set; } = new();

    /// <summary>Null (or a blank/"-" Name once normalized) means this service is ungrouped — no synthetic fallback row is created, the service's SubDepartmentId is simply null.</summary>
    public CatalogRowSubDepartment? SubDepartment { get; set; }

    public CatalogRowService Service { get; set; } = new();
}

public sealed class CatalogRowDepartment
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? HomeUrlEn { get; set; }
    public string? HomeUrlKn { get; set; }
}

public sealed class CatalogRowSubDepartment
{
    public string? Name { get; set; }
}

public sealed class CatalogRowService
{
    public string ServiceCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameKannada { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? DescriptionKannada { get; set; }
    public bool? IsSakalaEnabled { get; set; }
    public string? SakalaLabelRaw { get; set; }
    public int? SakalaDays { get; set; }
    public decimal? DepartmentFee { get; set; }
    public decimal? ServiceCharge { get; set; }

    /// <summary>Checklist-style ("Aadhaar Card", "Ration Card") — no URL. Paired with DocumentsKn by index where both exist; source data is not guaranteed to keep EN/KN lists the same length, so unmatched entries are kept EN/KN-only rather than dropped.</summary>
    public List<string> DocumentsEn { get; set; } = [];
    public List<string> DocumentsKn { get; set; } = [];
    public List<string> EligibilityEn { get; set; } = [];
    public List<string> EligibilityKn { get; set; } = [];
    public List<string> ProcedureEn { get; set; } = [];
    public List<string> ProcedureKn { get; set; } = [];
    public string? Jurisdiction { get; set; }
    public string? Category { get; set; }

    /// <summary>Raw, as the source has it — CatalogIngestionService normalizes this to 'Online'/'Offline'/'Both' before it ever reaches the DB.</summary>
    public string? ModeOfService { get; set; }

    public bool? IsMandatory { get; set; }
    public string? DepartmentServiceId { get; set; }
    public string? ServiceUrl { get; set; }

    /// <summary>Where "Apply now" sends the citizen for this service. Sourced from the xlsx's
    /// per-row "Dept home page url english/kannada" columns -- the only apply-style link the
    /// real source data actually fills in ("Deep Link URL" is present in the sheet but empty
    /// on every real row). Genuinely varies within a department when it has more than one
    /// portal (e.g. Revenue's AJSK sub-department vs. its main services), which is exactly
    /// why this lives per-service rather than only on CatalogRowDepartment.HomeUrlEn/Kn.</summary>
    public string? ApplyUrl { get; set; }
    public string? ApplyUrlKannada { get; set; }
}

public sealed record ValidationIssue(string Field, string Message);

public sealed record ImportBatchStatusDto(
    Guid Id,
    string SourceFilename,
    string SourceFormat,
    string UploadedBy,
    DateTimeOffset UploadedAt,
    string Status,
    int RowCount,
    int ErrorCount,
    int WarningCount,
    DateTimeOffset? PublishedAt,
    string? PublishedBy);

public sealed record ImportRowDto(
    Guid Id,
    int RowNumber,
    string? Action,
    IReadOnlyList<ValidationIssue> Errors,
    IReadOnlyList<ValidationIssue> Warnings,
    string DepartmentCode,
    string ServiceCode,
    string ServiceName);

public sealed record PublishResult(int Published, int Skipped);
