using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;

namespace SevaOne.Domain.Models;

public sealed class ApplicationEvent : BaseModel
{
    public Guid ApplicationId { get; set; }
    public ApplicationStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset At { get; set; }
}
