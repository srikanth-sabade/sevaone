using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>department_master. <see cref="Code"/> is the business "DEPARTMENT_ID" — unique, referenced by Apply URL handoff and every department-facing API call.</summary>
public sealed class Department : BaseModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PortalName { get; set; } = string.Empty;
    public string PortalDomain { get; set; } = string.Empty;
    public string ConnectorType { get; set; } = string.Empty;
    public bool IsIntegrated { get; set; }

    /// <summary>The department's own public website -- distinct from <see cref="PortalDomain"/>, which is where SevaOne redirects citizens for the apply handoff. Added in V22, not wired into this model until the Service Catalogue work.</summary>
    public string? DepartmentUrl { get; set; }
    public string? DepartmentUrlKannada { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
