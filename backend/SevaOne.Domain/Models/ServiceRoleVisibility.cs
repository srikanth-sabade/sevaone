namespace SevaOne.Domain.Models;

/// <summary>
/// discovery.service_role_visibility. A service with no row here for a given role is
/// genuinely absent from that role's query results -- a fact about the data, not a filter
/// applied after the fact. Pure join table, so it does not extend <see cref="Common.BaseModel"/>
/// -- the composite (ServiceId, RoleId) key doubles as the PK, no surrogate id.
/// </summary>
public sealed class ServiceRoleVisibility
{
    public Guid ServiceId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public string? GrantedBy { get; set; }
}
