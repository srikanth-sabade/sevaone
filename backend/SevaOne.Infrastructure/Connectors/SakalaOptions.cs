namespace SevaOne.Infrastructure.Connectors;

/// <summary>
/// Config for Karnataka's real, live Sakala acknowledgement/grievance-tracking API
/// (sakala.kar.nic.in) -- a public government status lookup, not a SevaOne-owned system.
/// Confirmed live: no auth/API key needed, just POST {gscno} and get the citizen's Sakala
/// record back. BaseUrl is configurable (not hardcoded) purely so a sandbox/staging endpoint
/// can be swapped in later if the department ever provides one -- this codebase's convention
/// for every other external integration (DigiLocker, WSO2, department connectors).
/// </summary>
public sealed class SakalaOptions
{
    public const string SectionName = "Sakala";

    public string BaseUrl { get; set; } = "https://sakala.kar.nic.in/ack_api";
}
