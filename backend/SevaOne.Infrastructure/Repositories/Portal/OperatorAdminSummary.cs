namespace SevaOne.Infrastructure.Repositories.Portal;

public sealed class OperatorAdminSummary
{
    public Guid Id { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string OperatorType { get; set; } = string.Empty;
    public string? OperatorName { get; set; }
    public string? MobileNo { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
