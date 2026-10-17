using SevaOne.Business.Portal.DTOs;
using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Business.Portal.Interfaces;

public interface ICitizenSessionService
{
    /// <summary>Null when the citizen has never signed in, otherwise their ProfileCompleted flag. Used to skip Email OTP for first-time / incomplete profiles.</summary>
    Task<bool?> GetProfileCompletedAsync(string? subject, string? username, CancellationToken ct);

    /// <summary>
    /// Bootstraps/updates the citizen row, then provisions/exchanges a real WSO2 identity for
    /// them — required, not soft-fail, here: this IS the citizen's login credential now (see
    /// AuthController's cookie handling), so if WSO2 can't be reached this call throws rather
    /// than silently degrading to some other token.
    /// </summary>
    Task<(Wso2CitizenTokens Tokens, string ReturnTo, bool ProfileCompleted)> EstablishAsync(
        Wso2UserInfo user, string returnTo, CancellationToken ct);
}
