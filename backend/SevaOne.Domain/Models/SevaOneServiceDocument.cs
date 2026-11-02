using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// sevaone_service_document — normalized document/URL rows the AI reads to answer questions
/// without asking the citizen, one per document per service. Superset of the legacy
/// <see cref="Scheme.RequiredDocumentsJson"/> blob: this table is what AI ingestion and document-URL
/// governance should read.
/// </summary>
public sealed class SevaOneServiceDocument : BaseModel
{
    public Guid SchemeId { get; set; }
    public string? DocumentName { get; set; }

    /// <summary>URL the AI reads for document detail.</summary>
    public string DocumentUrl { get; set; } = string.Empty;

    public string? DocumentType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
