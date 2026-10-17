namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Seva Sindhu's Sakala-token ESCOM integration (BESCOM/CESCOM/MESCOM) -- scoped strictly to
/// Service Catalogue 2.0's "Apply now" flow for those three departments, see
/// frontend/src/lib/sevaSindhuEscom.ts for the matching department allowlist. This is a
/// separate government system with its own token shape, not part of the SevaOne
/// apply-token/JWS flow used elsewhere in this codebase (PortalApplyService).
/// </summary>
public sealed class SevaSindhuEscomOptions
{
    public const string SectionName = "SevaSindhuEscom";

    public string TokenUrl { get; set; } = "https://sevasindhu.karnataka.gov.in/sakalatokenESCOMAPI_Live/api/token_generation";

    /// <summary>Despite the path containing "CESCOM", this is the one redirect destination used
    /// for BESCOM/CESCOM/MESCOM alike -- no separate per-department URL exists in the
    /// integration details supplied for this feature.</summary>
    public string RedirectUrl { get; set; } = "https://sevasindhu.karnataka.gov.in//SakalaTokenRedirection_Escom_Live/STBRedirectToCESCOM.aspx";

    /// <summary>TODO: confirm with Seva Sindhu what this value actually represents -- citizen
    /// ID, assisted-mode operator ID, department user ID, integration account ID, or a fixed
    /// system ID. Treated as one fixed configured value for now (matching the supplied example,
    /// which used one unchanging ID) rather than blocking this integration on that answer.
    /// Left blank here on purpose -- same fail-closed pattern as DigiLockerOptions.DeptKey --
    /// set via user-secrets/environment/appsettings.Development.json for real use.</summary>
    public string UserId { get; set; } = string.Empty;

    public string AssistedMode { get; set; } = "Y";
}
