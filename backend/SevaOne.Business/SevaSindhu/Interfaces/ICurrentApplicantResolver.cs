namespace SevaOne.Business.SevaSindhu.Interfaces;

/// <summary>
/// Resolves which User_Id a Seva Sindhu token-generation call should carry -- shared by both
/// SevaSindhuEscomService and SevaSindhuCatalog2Service since the resolution rule is identical
/// for both (only the redirect-URL shape differs between them, which stays separate). Never
/// trusts a client-supplied id: an authenticated operator's bound citizen is read server-side
/// off their own session; everyone else gets the fixed configured fallback.
/// </summary>
public interface ICurrentApplicantResolver
{
    Task<string> ResolveUserIdAsync(string fallbackUserId, CancellationToken ct);
}
