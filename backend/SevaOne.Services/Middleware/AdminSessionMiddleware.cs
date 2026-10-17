using System.Security.Claims;
using SevaOne.Infrastructure.Caching;

namespace SevaOne.Services.Middleware;

/// <summary>Catalog-admin counterpart of OperatorSessionMiddleware -- separate cookie (seva.admin.sid), separate Redis store, separate "SevaAdmin" identity added onto the same context.User a citizen and/or operator identity may already be sitting on.</summary>
public sealed class AdminSessionMiddleware
{
    public const string CookieName = "seva.admin.sid";
    private readonly RequestDelegate _next;

    public AdminSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAdminSessionStore store)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var sid) && !string.IsNullOrWhiteSpace(sid))
        {
            var session = await store.GetAsync(sid, context.RequestAborted);
            if (session is not null && session.ExpiresAt > DateTimeOffset.UtcNow)
            {
                var identity = new ClaimsIdentity(
                    [
                        new Claim("admin_id", session.AdminId.ToString()),
                        new Claim("admin_code", session.AdminCode),
                        new Claim("admin_session_id", session.SessionId),
                        new Claim(ClaimTypes.Name, session.AdminName ?? session.AdminCode)
                    ],
                    "SevaAdmin");
                context.User.AddIdentity(identity);
                context.Items["AdminSession"] = session;
            }
        }

        await _next(context);
    }
}
