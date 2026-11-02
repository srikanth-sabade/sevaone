namespace SevaOne.Domain.Models;

/// <summary>One escalation/complaint record within a Sakala acknowledgement (SakalaAckRecord's
/// FirstComplaint/SecondComplaint) -- see ISakalaClient's doc comment. All fields null when that
/// complaint slot was never used (the real API returns empty strings for an unused second
/// complaint, not a missing key).</summary>
public sealed class SakalaComplaint
{
    public string? ComplaintNo { get; set; }
    public DateOnly? ComplaintDate { get; set; }
    public DateOnly? ClosedDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusDescription { get; set; }
    public int? DaysPending { get; set; }
}

/// <summary>A citizen's real Sakala acknowledgement record, as looked up live from
/// sakala.kar.nic.in by GSC number -- see ISakalaClient. Independent of anything stored in
/// SevaOne's own orchestration.applications; this is the government's own record for the
/// service, which may have been applied for through any channel, not necessarily SevaOne.</summary>
public sealed class SakalaAckRecord
{
    public string GscNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string? OfficerName { get; set; }
    public string? SecretariatDepartmentName { get; set; }
    public DateOnly? SakalaDate { get; set; }
    public DateOnly? SakalaClosedDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public int? DaysPending { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public SakalaComplaint? FirstComplaint { get; set; }
    public SakalaComplaint? SecondComplaint { get; set; }
}
