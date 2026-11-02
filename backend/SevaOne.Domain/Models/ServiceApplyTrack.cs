using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// discovery.service_apply_track. One row per apply attempt -- real deep-link token mint OR
/// homepage-fallback redirect, both count as genuine apply intent. Never written for
/// department-integration test-token mints (testMode), which are synthetic QA traffic, not real
/// demand. Log table, not a counter column, so the count is always reconstructable and the same
/// data supports "how many total" today and "trend over time" / "by department" later.
/// </summary>
public sealed class ServiceApplyTrack : BaseModel
{
    public Guid? SchemeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? CitizenId { get; set; }
    public bool AssistedMode { get; set; }
    public bool IsHomepageFallback { get; set; }
    /// <summary>"catalogue" or "ai" - which apply flow produced this click. See
    /// V67__service_apply_track_source.sql.</summary>
    public string Source { get; set; } = "catalogue";
    public DateTimeOffset CreatedAt { get; set; }
}
