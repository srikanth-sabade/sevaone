using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SevaOne.Domain.Common;
using SevaOne.Services.Auth;

namespace SevaOne.Services.Middleware;

/// <summary>
/// Enforces that department-facing requests actually came through WSO2 APIM's department
/// gateway API, not straight to this backend port. APIM injects X-SevaOne-Dept-Gateway-Secret
/// via a header modification policy on every request it forwards; nothing else knows this
/// value. This runs BEFORE DeptMachineAuth (DeptController.TryResolveDepartment) — it answers
/// "did this genuinely pass through APIM," not "which department is this." APIM itself no
/// longer validates a WSO2 OAuth2 bearer for department traffic (removed — DeptMachineAuth
/// already fully identifies the department, so the bearer was a redundant second identity
/// check); this stamp plus network isolation (the backend not being reachable except via APIM)
/// replace it.
/// </summary>
public sealed class DeptGatewayAuthMiddleware
{
    public const string HeaderName = "X-SevaOne-Dept-Gateway-Secret";

    private const string DeptPathPrefix = "/api/v1/dept";

    private readonly RequestDelegate _next;

    public DeptGatewayAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<DeptGatewayAuthOptions> options, IHostEnvironment env)
    {
        if (!context.Request.Path.StartsWithSegments(DeptPathPrefix))
        {
            await _next(context);
            return;
        }

        var expected = options.Value.Secret;
        // Empty secret bypasses the check only in Development -- gated on the actual hosting
        // environment, not just "the secret happens to be empty" (an empty secret in any real
        // deployment means the secret failed to provision, which should still fail closed, not
        // silently open the "only APIM can reach the backend" perimeter).
        if (string.IsNullOrEmpty(expected) && env.IsDevelopment())
        {
            await _next(context);
            return;
        }

        var provided = context.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided) || !FixedTimeEquals(expected, provided))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                code = SevaErrorCodes.Gateway.MissingOrInvalidSecret,
                title = "Request did not come through the department gateway."
            });
            return;
        }

        await _next(context);
    }

    private static bool FixedTimeEquals(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
}
