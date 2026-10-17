namespace SevaOne.Infrastructure.ExternalAuth;

public sealed record Wso2GovAdminLoginResult(bool Success, bool InGovAdminGroup);

/// <summary>
/// Real WSO2 IS identity for the government admin panel -- mirrors IWso2OperatorIdentityClient.
/// Superadmin creates the gov_admin_master row first (tier/scope already assigned, no WSO2
/// account yet); ProvisionAsync is called by GovAdminClaimService once the new admin completes
/// their first-time-login mobile-OTP claim and sets their own password -- see
/// V69__gov_admin_master_claim_flow.sql.
/// </summary>
public interface IWso2GovAdminIdentityClient
{
    /// <summary>Creates the WSO2 IS account for a newly-claimed gov admin, with the password
    /// they chose, and assigns them to the SevaOne_GovAdmin RBAC group. Success=false on any
    /// failure (username conflict, WSO2 unreachable) -- not soft-fail, GovAdminClaimService must
    /// not consider the claim successful if this fails, since there'd be nowhere else for a
    /// credential to live. ErrorDetail carries WSO2's own rejection reason when it's safe to show
    /// a user (SCIM2 password policy violations) -- null otherwise.</summary>
    Task<(bool Success, string? ErrorDetail)> ProvisionAsync(string scimUsername, string name, string mobile, string password, CancellationToken ct);

    /// <summary>Verifies scimUsername/password via the OAuth2 password grant, then checks
    /// SevaOne_GovAdmin group membership via a separate SCIM2 read (see
    /// Wso2GovAdminIssuerOptions.GroupName's doc comment for why). Success=false means bad
    /// credentials; Success=true with InGovAdminGroup=false means valid WSO2 credentials but not
    /// a gov admin -- both are "reject the login" to the caller, kept separate only for
    /// logging/diagnosis.</summary>
    Task<Wso2GovAdminLoginResult> LoginAsync(string scimUsername, string password, CancellationToken ct);
}
