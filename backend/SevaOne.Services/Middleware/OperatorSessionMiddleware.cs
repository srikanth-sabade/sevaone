using System.Security.Claims;
using SevaOne.Infrastructure.Caching;

namespace SevaOne.Services.Middleware;

/// <summary>Operator-session counterpart of Wso2CitizenAuthMiddleware -- separate cookie (seva.op.sid), separate Redis store, separate "SevaOperator" identity added onto the same context.User a citizen identity may already be sitting on.</summary>
public sealed class OperatorSessionMiddleware
{
    public const string CookieName = "seva.op.sid";
    private readonly RequestDelegate _next;

    public OperatorSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOperatorSessionStore store)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var sid) && !string.IsNullOrWhiteSpace(sid))
        {
            var session = await store.GetAsync(sid, context.RequestAborted);
            if (session is not null && session.ExpiresAt > DateTimeOffset.UtcNow)
            {
                var identity = new ClaimsIdentity(
                    [
                        new Claim("operator_id", session.OperatorId.ToString()),
                        new Claim("operator_code", session.OperatorCode),
                        new Claim("op_session_id", session.SessionId),
                        new Claim(ClaimTypes.Name, session.OperatorName ?? session.OperatorCode)
                    ],
                    "SevaOperator");
                context.User.AddIdentity(identity);
                context.Items["OperatorSession"] = session;
            }
        }

        await _next(context);
    }
}
