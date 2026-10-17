namespace SevaOne.Infrastructure.ExternalAuth;

public sealed record Wso2OperatorLoginResult(bool Success, bool InOperatorGroup);

/// <summary>
/// Real WSO2 IS identity for operators -- unlike IWso2CitizenIdentityClient, this is the
/// PRIMARY credential (operators authenticate directly against WSO2, not a SevaOne-owned
/// password), not a soft-fail add-on. See Wso2OperatorIssuerOptions.
/// </summary>
public interface IWso2OperatorIdentityClient
{
    /// <summary>Creates the WSO2 IS account for a newly self-claimed operator, with the
    /// password they chose (unlike citizens' random throwaway password -- operators actually
    /// log in with this), and assigns them to the operator RBAC group. Success=false on any
    /// failure (username conflict, WSO2 unreachable) -- unlike citizen provisioning, this is
    /// NOT soft-fail; the caller (OperatorClaimService) must not consider the claim successful
    /// if this fails, since there'd be nowhere else for a credential to live. ErrorDetail carries
    /// WSO2's own rejection reason when it's safe to show a user (currently: SCIM2 password
    /// policy violations -- e.g. WSO2 requiring an uppercase letter our own StartAsync check
    /// didn't anticipate, which is exactly what happened before that check existed) -- null for
    /// failures that aren't (WSO2 unreachable, a group-membership PATCH failing after the user
    /// already exists, etc.), where OperatorClaimService's generic message is the right call.</summary>
    Task<(bool Success, string? ErrorDetail)> ProvisionAsync(string scimUsername, string name, string mobile, string password, CancellationToken ct);

    /// <summary>Verifies scimUsername/password via the OAuth2 password grant, then checks
    /// operator-group membership via a separate SCIM2 read (see Wso2OperatorIssuerOptions'
    /// GroupName doc comment for why membership isn't read off the token itself). Success=false
    /// means bad credentials; Success=true with InOperatorGroup=false means valid WSO2
    /// credentials but not (or no longer) an operator -- both are "reject the login" to the
    /// caller, kept separate only for logging/diagnosis.</summary>
    Task<Wso2OperatorLoginResult> LoginAsync(string scimUsername, string password, CancellationToken ct);
}
