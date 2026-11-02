using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

public sealed class NotificationMessage : BaseModel
{
    public Guid? CitizenId { get; set; }
    public string Channel { get; set; } = "log";
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
