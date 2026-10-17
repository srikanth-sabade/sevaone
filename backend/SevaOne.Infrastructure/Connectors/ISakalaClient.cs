using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Connectors;

/// <summary>
/// Karnataka's real Sakala acknowledgement/grievance-tracking API (sakala.kar.nic.in) -- looks up
/// a citizen's Sakala record (and any escalation complaints filed against it) by GSC number
/// ("Sakala No", the acknowledgement number issued when any Sakala-guaranteed service is
/// applied for, through any channel). This is a live, real government system SevaOne does not
/// own; used today as ApplicationStatusService.TrackAsync's fallback for a reference that isn't
/// a SevaOne sevaReference -- see that method's doc comment for why (SevaOne doesn't yet
/// capture a real GSC number into orchestration.applications.department_reference for any
/// live application, so a citizen's only way to check today is entering the GSC number
/// themselves).
/// </summary>
public interface ISakalaClient
{
    /// <summary>Null when the GSC number doesn't resolve to a real Sakala record -- confirmed
    /// live that an unrecognized gscno returns an HTTP 500 with a raw ADO.NET exception message
    /// (not a clean 404/empty array), so any non-success response or an empty result array is
    /// treated as "not found", never surfaced to the caller as an error.</summary>
    Task<SakalaAckRecord?> GetAckDataAsync(string gscNo, CancellationToken ct);
}
