namespace SevaOne.Domain.Models;

public sealed class GovAdminImportBatch
{
    public Guid Id { get; set; }
    public string SourceFilename { get; set; } = string.Empty;
    public string SourceFormat { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public string Status { get; set; } = "validating";
    public int RowCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
}
