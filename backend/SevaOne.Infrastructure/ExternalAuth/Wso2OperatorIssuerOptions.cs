namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Config for operator authentication against a REAL WSO2 IS account -- unlike citizens (whose
/// WSO2 identity is a soft-fail, best-effort add-on after they've already logged in some other
/// way), operators authenticate directly with WSO2 via the OAuth2 password grant. See
/// backend/docs/WSO2_IS_ROADMAP.md section 2.3 and Docker-deploy/wso2/provision.sh.
/// SCIM base URL/admin credentials are shared with the citizen issuer (same WSO2 IS instance) --
/// see Wso2OperatorIdentityClient's constructor.
/// </summary>
public sealed class Wso2OperatorIssuerOptions
{
    public const string SectionName = "Wso2OperatorIssuer";

    public string TokenEndpoint { get; set; } = "https://localhost:9443/oauth2/token";

    /// <summary>OAuth2 client credentials of the dedicated WSO2 application registered for the operator password grant -- see Docker-deploy/wso2/provision.sh.</summary>
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// SCIM2 group whose membership IS the operator RBAC check -- see Wso2OperatorIdentityClient's
    /// doc comment for why this is checked via a plain SCIM2 GET rather than a token/id_token
    /// claim (WSO2 IS 7.3.0 doesn't surface it there without deeper claim-dialect config that
    /// wasn't worth the complexity for a single group).
    /// </summary>
    public string GroupName { get; set; } = "SevaOne_Operator";
}
