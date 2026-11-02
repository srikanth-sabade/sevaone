using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SevaOne.Services.Auth;

/// <summary>Thin auth-scheme wrapper: OperatorSessionMiddleware already builds the ClaimsPrincipal from the seva.op.sid cookie before this handler runs, so it only needs to report success/failure for [Authorize(AuthenticationSchemes = "SevaOperator")]. Mirrors SevaAuthHandler for the citizen scheme.</summary>
public sealed class OperatorAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public OperatorAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User.Identities.Any(i => i.AuthenticationType == "SevaOperator" && i.IsAuthenticated))
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Context.User, "SevaOperator")));
        }

        return Task.FromResult(AuthenticateResult.Fail("Not authenticated."));
    }
}
