using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SevaOne.Services.Auth;

namespace SevaOne.Services.Middleware;

/// <summary>
/// Enforces that citizen-facing requests actually came through WSO2 APIM's citizen gateway API,
/// not straight to this backend port. APIM injects X-SevaOne-Citizen-Gateway-Secret via a header
/// modification policy on every request it forwards; nothing else knows this value. This is the
/// second, independent lock behind network isolation (the backend not being reachable from
/// outside APIM in the first place) -- same defense-in-depth shape DeptMachineAuth already gives
/// department traffic, just a single fixed secret instead of a per-department lookup table.
/// </summary>
public sealed class CitizenGatewayAuthMiddleware
{
    public const string HeaderName = "X-SevaOne-Citizen-Gateway-Secret";

    // Everything a citizen's browser calls. Department (/api/v1/dept/*), operator
    // (/api/v1/operator/*), and internal webhook (/api/v1/internal/*) traffic each have their
    // own, separate auth layer and are deliberately NOT gated here.
    private static readonly string[] CitizenPathPrefixes =
    [
        "/api/v1/auth",
        "/api/v1/me",
        "/api/v1/departments",
        "/api/v1/schemes",
        "/api/v1/search",
        "/api/v1/applications",
        "/api/v1/track",
        "/api/v1/apply"
    ];

    private readonly RequestDelegate _next;

    public CitizenGatewayAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<CitizenGatewayAuthOptions> options, IHostEnvironment env)
    {
        var path = context.Request.Path;
        // DigiLocker/CeG redirects the citizen's browser here. That GET cannot carry APIM's
        // injected gateway secret unless CallbackUrl is the public citizen-gateway URL — exclude
        // so local CallbackUrl on the backend port also works. Replay is still blocked by Redis
        // one-time AppParam + CeG txn lookup.
        if (path.StartsWithSegments("/api/v1/auth/digilocker/callback"))
        {
            await _next(context);
            return;
        }

        var isCitizenPath = CitizenPathPrefixes.Any(prefix => path.StartsWithSegments(prefix));
        if (!isCitizenPath)
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
            await context.Response.WriteAsJsonAsync(new { title = "Request did not come through the citizen gateway." });
            return;
        }

        await _next(context);
    }

    private static bool FixedTimeEquals(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
}
