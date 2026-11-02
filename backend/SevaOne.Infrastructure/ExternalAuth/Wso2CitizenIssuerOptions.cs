namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Config for SevaOne's own JWT-bearer token exchange (RFC 7523) with WSO2 IS. This is how a
/// DigiLocker/mobile-OTP-verified citizen gets a REAL, introspectable WSO2 access/refresh token
/// pair without ever entering a WSO2 password, so WSO2 IS can serve as Karnataka's statewide
/// identity provider beyond just SevaOne. This token pair, held in an HttpOnly cookie, IS the
/// citizen's live credential — see Wso2CitizenAuthMiddleware.
/// </summary>
public sealed class Wso2CitizenIssuerOptions
{
    public const string SectionName = "Wso2CitizenIssuer";

    /// <summary>RS256 private key, PEM format. Real value lives only in user-secrets/environment — never checked into appsettings.json.</summary>
    public string SigningKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// Must match the exact "name" (NOT "alias") of the Trusted Token Issuer identity provider
    /// registered in WSO2 IS — confirmed empirically: WSO2's JWTBearerGrantHandler resolves the
    /// assertion's `iss` claim via IdentityProviderManager, which looks IdPs up by their name
    /// column. Sending the alias instead fails with invalid_grant: "No registered identity
    /// provider found for the JWT with issuer name : &lt;value&gt;".
    /// </summary>
    public string Issuer { get; set; } = "SevaOne Citizen Issuer";

    /// <summary>
    /// Must match the exact "alias" of the same Trusted Token Issuer IdP — confirmed
    /// empirically: WSO2 validates the assertion's `aud` claim against the IdP's configured
    /// "tokenEndpoint Alias" (the alias field), NOT the literal /oauth2/token URL. Sending the
    /// token endpoint URL instead fails with invalid_grant: "None of the audience values matched
    /// the tokenEndpoint Alias &lt;value&gt;".
    /// </summary>
    public string Audience { get; set; } = "sevaone-citizen-identity";

    public string TokenEndpoint { get; set; } = "https://localhost:9443/oauth2/token";

    /// <summary>OAuth2 client credentials of the dedicated WSO2 application registered for this JWT-bearer exchange — used as Basic auth on the token endpoint call.</summary>
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public string ScimBaseUrl { get; set; } = "https://localhost:9443";

    /// <summary>Dev-only basic-auth admin credentials for SCIM2 provisioning. Replace with a dedicated least-privilege SCIM service account before production.</summary>
    public string ScimAdminUsername { get; set; } = "admin";
    public string ScimAdminPassword { get; set; } = "admin";

    public bool AcceptInsecureCertificates { get; set; } = true;
}
