namespace SevaOne.Domain.Models;

/// <summary>The persisted public service notice. There is at most one row; the database keeps
/// the singleton invariant so every public page sees the same operational message.</summary>
public sealed class ServiceNotice
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ServiceNoticeHistory
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool WasActive { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset SavedAt { get; set; }
}
