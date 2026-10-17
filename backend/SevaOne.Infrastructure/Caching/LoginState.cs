namespace SevaOne.Infrastructure.Caching;

/// <summary>Pending OIDC/password-grant login state held between the redirect/submit and the callback/OTP-verify step. Also used by the live mobile-OTP challenge (PendingMobile/OtpHash/ReturnTo). Citizens have no session object of their own anymore -- their WSO2 access token, held in an HttpOnly cookie, IS the credential; see Wso2CitizenAuthMiddleware / IWso2TokenIntrospectionCache.</summary>
public sealed class LoginState
{
    public string CodeVerifier { get; set; } = string.Empty;
    public string ReturnTo { get; set; } = "/";
    public string Nonce { get; set; } = string.Empty;

    /// <summary>Pending API login (password grant) awaiting Email OTP on the Seva login UI.</summary>
    public string? PendingAccessToken { get; set; }
    public string? PendingRefreshToken { get; set; }
    public string? OtpHash { get; set; }
    public string? PendingSubject { get; set; }
    public string? PendingUsername { get; set; }
    public string? PendingName { get; set; }
    public string? PendingEmail { get; set; }
    public string? PendingMobile { get; set; }

    /// <summary>Operator self-claim (OperatorClaimService) staging -- the whitelisted user_id
    /// being claimed and the password to set once the OTP above verifies. Held as plaintext,
    /// unlike everything else in this class -- WSO2 IS's SCIM2 user-create API needs the real
    /// password (it does its own hashing/storage), there's no pre-hashed form it accepts.
    /// Bounded exposure: single-use (TakeLoginStateAsync deletes it) and this same 10-minute
    /// TTL as the OTP challenge itself.</summary>
    public long? PendingOperatorUserId { get; set; }
    public string? PendingOperatorPassword { get; set; }

    /// <summary>Gov-admin first-time-login claim (GovAdminClaimService) staging -- same
    /// plaintext-password rationale as the operator fields above.</summary>
    public string? PendingGovAdminUsername { get; set; }
    public string? PendingGovAdminPassword { get; set; }
}
