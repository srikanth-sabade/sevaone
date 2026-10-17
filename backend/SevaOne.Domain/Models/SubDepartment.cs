using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// discovery.sub_department_master. Not every department has sub-departments -- a service's
/// <see cref="Scheme.SubDepartmentId"/> is nullable, and there is deliberately no synthetic
/// "fallback" row for that case; a null SubDepartmentId on the service *is* the ungrouped state.
/// </summary>
public sealed class SubDepartment : BaseModel
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Admin-controlled ordering for the catalog UI's sub-department rail -- never touched by ingestion re-imports.</summary>
    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
