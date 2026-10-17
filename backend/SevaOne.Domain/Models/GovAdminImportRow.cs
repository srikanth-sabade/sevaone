namespace SevaOne.Domain.Models;

public sealed class GovAdminImportRow
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public string? Action { get; set; }
    public string ValidationErrors { get; set; } = "[]";
    public string ValidationWarnings { get; set; } = "[]";
}
