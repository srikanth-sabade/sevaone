using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

public sealed class AuditLog : BaseModel
{
    public Guid? CitizenId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset At { get; set; }
}
