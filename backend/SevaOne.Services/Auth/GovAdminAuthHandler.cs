using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SevaOne.Services.Auth;

/// <summary>Thin auth-scheme wrapper: GovAdminSessionMiddleware already builds the
/// ClaimsPrincipal from the seva.govadmin.sid cookie before this handler runs, so it only needs
/// to report success/failure for [Authorize(AuthenticationSchemes = "SevaGovAdmin")]. Mirrors
/// OperatorAuthHandler/AdminAuthHandler. Named "SevaGovAdmin", not "SevaAdmin" -- that scheme
/// name is already taken by the unrelated PBKDF2-based CatalogAdminController persona.</summary>
public sealed class GovAdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public GovAdminAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User.Identities.Any(i => i.AuthenticationType == "SevaGovAdmin" && i.IsAuthenticated))
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Context.User, "SevaGovAdmin")));
        }

        return Task.FromResult(AuthenticateResult.Fail("Not authenticated."));
    }
}
