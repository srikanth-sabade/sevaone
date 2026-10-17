using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SevaOne.Services.Auth;

/// <summary>Thin auth-scheme wrapper: Wso2CitizenAuthMiddleware already builds the ClaimsPrincipal from the seva.wso2.at cookie before this handler runs, so it only needs to report success/failure for [Authorize]. Checks Identities (plural) by AuthenticationType, not the bare .Identity property -- context.User can carry a second "SevaOperator" identity at the same time (see Wso2CitizenAuthMiddleware), and .Identity alone would only reflect whichever identity happens to be primary.</summary>
public sealed class SevaAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SevaAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User.Identities.Any(i => i.AuthenticationType == "Seva" && i.IsAuthenticated))
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Context.User, "Seva")));
        }

        return Task.FromResult(AuthenticateResult.Fail("Not authenticated."));
    }
}
