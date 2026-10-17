using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SevaOne.Services.Auth;

/// <summary>Thin auth-scheme wrapper: AdminSessionMiddleware already builds the ClaimsPrincipal from the seva.admin.sid cookie before this handler runs, so it only needs to report success/failure for [Authorize(AuthenticationSchemes = "SevaAdmin")]. Mirrors OperatorAuthHandler.</summary>
public sealed class AdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public AdminAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User.Identities.Any(i => i.AuthenticationType == "SevaAdmin" && i.IsAuthenticated))
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Context.User, "SevaAdmin")));
        }

        return Task.FromResult(AuthenticateResult.Fail("Not authenticated."));
    }
}
