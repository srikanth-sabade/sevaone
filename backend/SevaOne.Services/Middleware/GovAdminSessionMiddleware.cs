using System.Security.Claims;
using SevaOne.Infrastructure.Caching;

namespace SevaOne.Services.Middleware;

/// <summary>Gov-admin-panel counterpart of OperatorSessionMiddleware/AdminSessionMiddleware --
/// separate cookie (seva.govadmin.sid), separate Redis store, separate "SevaGovAdmin" identity
/// added onto the same context.User a citizen/operator/admin identity may already be sitting
/// on. Tier/district/taluk are baked into the claims from the cached GovAdminSession (see its
/// doc comment) -- ICurrentGovAdmin reads them straight off context.User, no extra Redis or
/// Postgres round trip per request.</summary>
public sealed class GovAdminSessionMiddleware
{
    public const string CookieName = "seva.govadmin.sid";
    private readonly RequestDelegate _next;

    public GovAdminSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IGovAdminSessionStore store)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var sid) && !string.IsNullOrWhiteSpace(sid))
        {
            var session = await store.GetAsync(sid, context.RequestAborted);
            if (session is not null && session.ExpiresAt > DateTimeOffset.UtcNow)
            {
                var claims = new List<Claim>
                {
                    new("gov_admin_id", session.GovAdminId.ToString()),
                    new("gov_admin_wso2_subject", session.Wso2Subject),
                    new("gov_admin_tier", session.Tier),
                    new("gov_admin_session_id", session.SessionId),
                    new(ClaimTypes.Name, session.DisplayName)
                };
                if (session.AssignedDistrict is { } district)
                {
                    claims.Add(new Claim("gov_admin_district", district));
                }
                if (session.AssignedTaluk is { } taluk)
                {
                    claims.Add(new Claim("gov_admin_taluk", taluk));
                }
                if (session.AssignedDepartment is { } department)
                {
                    claims.Add(new Claim("gov_admin_department", department));
                }

                context.User.AddIdentity(new ClaimsIdentity(claims, "SevaGovAdmin"));
                context.Items["GovAdminSession"] = session;
            }
        }

        await _next(context);
    }
}
