using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Infrastructure.Repositories.Portal;

namespace SevaOne.Services.Middleware;

/// <summary>
/// Replaces SevaSessionMiddleware: the citizen's WSO2 access token (held in an HttpOnly cookie,
/// never readable by page JS) IS the login credential now, not a SevaOne-issued session id.
/// Validated via IWso2TokenIntrospectionCache (Redis-cached WSO2 introspection, not a live WSO2
/// call on every request) rather than a SevaOne-owned Redis session lookup — there is no
/// SevaOne-side session store for citizens anymore. Builds the SAME ClaimsIdentity shape the old
/// middleware did so ICurrentCitizen / [Authorize(AuthenticationSchemes = "Seva")] / SevaAuthHandler
/// need no changes.
/// </summary>
public sealed class Wso2CitizenAuthMiddleware
{
    public const string AccessTokenCookieName = "seva.wso2.at";
    public const string RefreshTokenCookieName = "seva.wso2.rt";

    private readonly RequestDelegate _next;

    public Wso2CitizenAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IWso2TokenIntrospectionCache introspection, IPortalRepository portal)
    {
        if (context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var token) && !string.IsNullOrWhiteSpace(token))
        {
            var result = await introspection.ValidateAsync(token, context.RequestAborted);
            if (result.Active
                && !string.IsNullOrWhiteSpace(result.Username)
                && result.Username.StartsWith("sevaone-", StringComparison.Ordinal)
                && long.TryParse(result.Username.AsSpan("sevaone-".Length), out var sevaOneUserId))
            {
                var citizen = await portal.GetCitizenBySevaOneUserIdAsync(sevaOneUserId, context.RequestAborted);
                if (citizen is not null)
                {
                    // Not a stored session id -- a stable-per-token fingerprint, purely so audit
                    // rows / ICurrentCitizen.SessionId keep meaning "which login produced this
                    // request" without SevaOne having to store anything about the token itself.
                    var tokenFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
                    var identity = new ClaimsIdentity(
                        [
                            new Claim("citizen_id", citizen.Id.ToString()),
                            new Claim("session_id", tokenFingerprint),
                            new Claim(ClaimTypes.Name, citizen.Name),
                            new Claim("seva_id", citizen.SevaId)
                        ],
                        "Seva");
                    // AddIdentity, not a wholesale replace of context.User: a browser can carry
                    // both a citizen credential AND an operator session cookie at once
                    // (assisted-mode counter) -- OperatorSessionMiddleware adds its own identity
                    // to this same principal, and each must survive the other running.
                    context.User.AddIdentity(identity);
                }
            }
        }

        await _next(context);
    }
}
