using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// dept_user_ref (optional/conceptual) — a thin cache of "this citizen is known to this
/// department" for support/analytics only. Departments own their own user masters; SevaOne
/// does not attempt to mirror them.
/// </summary>
public sealed class DeptUserRef : BaseModel
{
    public Guid DepartmentId { get; set; }
    public Guid CitizenId { get; set; }
    public Guid? OperatorId { get; set; }
    public bool AssistedMode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
