namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Seva Sindhu's general Sakala-token flow for Service Catalogue 2.0's "Apply now" -- every
/// Catalog 2.0 department EXCEPT BESCOM/CESCOM/MESCOM (their own separate flow, see
/// SevaSindhuEscomOptions -- a different API path and a different redirect target) and
/// Revenue Department (its services carry `rich` detail and render through a completely
/// different frontend panel, CatalogServiceDetailPanel, so they never reach the component
/// that calls this at all). Deliberately a parallel, non-shared client from the ESCOM one --
/// the request/response shape is identical, but the redirect URL carries two extra query
/// params (KioskType, ServiceID) that ESCOM's doesn't, so a forced shared abstraction would
/// cost more than the small duplication it would save.
/// </summary>
public sealed class SevaSindhuCatalog2Options
{
    public const string SectionName = "SevaSindhuCatalog2";

    public string TokenUrl { get; set; } = "https://sevasindhu.karnataka.gov.in/sakalatokenAPI_Live/api/token_generation";

    public string RedirectUrl { get; set; } = "https://sevasindhu.karnataka.gov.in/SakalaTokenRedirection_Live/STBRedirectToDepartment.aspx";

    /// <summary>TODO: same open question as SevaSindhuEscomOptions.UserId -- confirm with Seva
    /// Sindhu what this value represents. Left blank here on purpose, same fail-closed pattern
    /// as DigiLocker/Escom -- set via user-secrets/environment/appsettings.Development.json.</summary>
    public string UserId { get; set; } = string.Empty;

    public Dictionary<string, string> ServiceRedirectUrls { get; set; } = new()
    {
        ["2710"] = "https://sevasindhu.karnataka.gov.in/TokenBased_CSG/STBRedirectToCSG.aspx"
    };

    public string AssistedMode { get; set; } = "Y";

    /// <summary>Sent as-is in the redirect URL's KioskType param -- the supplied integration
    /// example always used "Y", with no further explanation of what other values mean.</summary>
    public string KioskType { get; set; } = "Y";
}
