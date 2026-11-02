using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;

namespace SevaOne.Domain.Models;

public sealed class ServiceApplication : BaseModel
{
    public string SevaReference { get; set; } = string.Empty;
    public string? DepartmentReference { get; set; }
    public Guid CitizenId { get; set; }
    public Guid SchemeId { get; set; }
    public Guid DepartmentId { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset SakalaDueAt { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string? District { get; set; }
    public string? Taluk { get; set; }
}
