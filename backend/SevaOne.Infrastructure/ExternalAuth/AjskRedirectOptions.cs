namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Service Plus -> SevaSindhu .NET redirect for AJSK (Atal Jana Snehi Kendra), per section 1
/// of Revenue_Services_Integration_Documentation.docx. Unlike the Sakala-token flows
/// (SevaSindhuEscomOptions/SevaSindhuCatalog2Options), there is no token API call here -- it's
/// a plain redirect carrying only UserId and RaisedLoc, and it isn't per-service (no
/// ServiceID/Service_Id param in the supplied example), so this covers every AJSK service the
/// same way. Scoped to operator-assisted apply only -- see Scheme.IntegrationMethod and
/// PortalApplyService.MintApplyTokenAsync; citizen apply on an AJSK service is unaffected and
/// keeps using DeeplinkCitizenUrl like every other service.
///
/// UserId is NOT configured here -- confirmed it's the ASSISTING OPERATOR's own whitelist id
/// (portal.operator_whitelist.user_id), not the citizen being assisted, passed into
/// AjskRedirectService.BuildRedirectUrl per call rather than read from fixed config.
/// </summary>
public sealed class AjskRedirectOptions
{
    public const string SectionName = "AjskRedirect";

    public string RedirectUrl { get; set; } = "https://sevasindhu.karnataka.gov.in/NKFive_Integration_App/NK5RedirectPage.aspx";

    /// <summary>"The location/context from which the service request was raised" -- confirmed
    /// value is "E" (the integration doc's own example showed "E / J" with no further
    /// explanation of what either meant; "E" is the one to use). Left blank by default here --
    /// same fail-closed pattern as the Sakala-family UserId fields -- set via
    /// user-secrets/environment/appsettings.Development.json/the deployment's own config for
    /// real use.</summary>
    public string RaisedLoc { get; set; } = "E";
}
