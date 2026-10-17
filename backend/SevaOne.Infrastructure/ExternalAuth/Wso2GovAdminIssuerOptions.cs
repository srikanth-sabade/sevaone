namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Config for gov-admin panel authentication against a REAL WSO2 IS account -- same existing
/// WSO2 IS userstore as citizens/operators (not a secondary userstore), password grant, gated
/// by SCIM2 group membership. See Wso2OperatorIssuerOptions (this mirrors it exactly) and
/// Docker-deploy/wso2/provision.sh for the OAuth app + SCIM2 group this points at.
/// SCIM base URL/admin credentials are shared with the citizen issuer (same WSO2 IS instance) --
/// see Wso2GovAdminIdentityClient's constructor.
/// </summary>
public sealed class Wso2GovAdminIssuerOptions
{
    public const string SectionName = "Wso2GovAdminIssuer";

    public string TokenEndpoint { get; set; } = "https://localhost:9443/oauth2/token";

    /// <summary>OAuth2 client credentials of the dedicated WSO2 application registered for the gov-admin password grant -- see Docker-deploy/wso2/provision.sh.</summary>
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// SCIM2 group whose membership IS the gov-admin coarse gate -- checked via a plain SCIM2
    /// GET, not a token/id_token claim, for the same reason as Wso2OperatorIssuerOptions.GroupName
    /// (confirmed live: WSO2 IS 7.3.0 doesn't surface group membership in the token regardless of
    /// requested claims). Fine-grained tier/district/taluk RBAC lives in
    /// portal.gov_admin_master, not WSO2, for the same reason.
    /// </summary>
    public string GroupName { get; set; } = "SevaOne_GovAdmin";
}
