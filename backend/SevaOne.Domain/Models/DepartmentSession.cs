using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

public sealed class DepartmentSession : BaseModel
{
    public Guid CitizenId { get; set; }
    public Guid DepartmentId { get; set; }
    public string EncryptedPayload { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
