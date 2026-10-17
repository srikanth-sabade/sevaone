namespace SevaOne.Business.SevaSindhu.Interfaces;

/// <summary>See AjskRedirectOptions -- builds the Service Plus -> SevaSindhu .NET AJSK redirect
/// URL. Not per-service (the integration doc's example carries no ServiceID param). UserId is
/// the ASSISTING OPERATOR's own whitelist id (portal.operator_whitelist.user_id), not the
/// citizen being assisted -- confirmed explicitly, not a fixed config value either.</summary>
public interface IAjskRedirectService
{
    string BuildRedirectUrl(string userId);
}
