using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Services.Auth;
using SevaOne.Services.Middleware;

namespace SevaOne.Services.Controllers;

[Route("api/v1")]
public sealed class AuthController : SevaControllerBase
{
    private readonly IPortalAuthService _auth;
    private readonly IWso2CitizenIdentityClient _wso2Identity;
    private readonly CookieSettings _cookies;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _log;

    public AuthController(
        IPortalAuthService auth,
        IWso2CitizenIdentityClient wso2Identity,
        IOptions<CookieSettings> cookies,
        IConfiguration config,
        ILogger<AuthController> log)
    {
        _auth = auth;
        _wso2Identity = wso2Identity;
        _cookies = cookies.Value;
        _config = config;
        _log = log;
    }

    [EnableRateLimiting("auth-start")]
    [HttpPost("auth/digilocker/start")]
    public async Task<IActionResult> DigiLockerStart([FromBody] DigiLockerStartBody body, CancellationToken ct)
    {
        var result = await _auth.StartDigiLockerLoginAsync(new StartDigiLockerLoginRequest(body.Flow, body.ReturnTo, body.PendingId), ct);
        return Ok(new { redirectUrl = result.RedirectUrl });
    }

    /// <summary>
    /// CeG/DigiLocker redirects the citizen's browser here. Must be a top-level GET so HttpOnly
    /// cookies can be set (SameSite=Lax). APIM sevaone-citizen must list this path as authType None.
    /// </summary>
    [HttpGet("auth/digilocker/callback")]
    public async Task<IActionResult> DigiLockerCallback(
        [FromQuery] string? status,
        [FromQuery] string? txnid,
        [FromQuery] string? appparam,
        CancellationToken ct)
    {
        var frontend = (_config["Frontend:PublicOrigin"] ?? "http://localhost:5174").TrimEnd('/');
        try
        {
            var result = await _auth.CompleteDigiLockerLoginAsync(
                new CompleteDigiLockerLoginRequest(txnid ?? "", appparam ?? "", status), ct);
            AppendTokenCookies(result.Tokens);
            return Redirect(frontend + result.ReturnTo);
        }
        catch (ConflictException ex)
        {
            return Redirect(frontend + "/login?loginError=" + Uri.EscapeDataString(ex.Message));
        }
        catch (ForbiddenException ex)
        {
            return Redirect(frontend + "/login?loginError=" + Uri.EscapeDataString(ex.Message));
        }
        catch (DomainException ex)
        {
            return Redirect(frontend + "/login?loginError=" + Uri.EscapeDataString(ex.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Anything else (a CeG API failure/timeout/malformed response from
            // DigiLockerClient, most likely) is not safe to show verbatim -- unlike the three
            // known types above, its message wasn't written to be user-facing. Log the real
            // exception, show a generic one, but still redirect: this is a browser landing on
            // a GET after a real navigation, not an API caller that can retry on a raw 500.
            _log.LogError(ex, "DigiLocker callback failed unexpectedly");
            return Redirect(frontend + "/login?loginError=" + Uri.EscapeDataString("DigiLocker sign-in failed. Please try again."));
        }
    }

    [EnableRateLimiting("auth-start")]
    [HttpPost("auth/mobile/start")]
    public async Task<IActionResult> MobileStart([FromBody] MobileStartBody body, CancellationToken ct)
    {
        var result = await _auth.StartMobileLoginAsync(new StartMobileOtpRequest(body.Mobile), ct);
        return Ok(new { challengeId = result.ChallengeId });
    }

    [EnableRateLimiting("auth-credential")]
    [HttpPost("auth/mobile/verify")]
    public async Task<IActionResult> MobileVerify([FromBody] MobileVerifyBody body, CancellationToken ct)
    {
        var result = await _auth.VerifyMobileLoginAsync(new VerifyMobileOtpRequest(body.ChallengeId, body.Otp), ct);
        if (result.Found && result.Tokens is not null)
        {
            AppendTokenCookies(result.Tokens);
        }

        return Ok(new { found = result.Found, mobile = result.Mobile, returnTo = result.ReturnTo, digiLockerPendingId = result.DigiLockerPendingId });
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(Wso2CitizenAuthMiddleware.AccessTokenCookieName, out var accessToken);
        await _auth.LogoutAsync(new LogoutRequest(accessToken), ct);
        Response.Cookies.Delete(Wso2CitizenAuthMiddleware.AccessTokenCookieName, new CookieOptions { Path = _cookies.Path });
        Response.Cookies.Delete(Wso2CitizenAuthMiddleware.RefreshTokenCookieName, new CookieOptions { Path = _cookies.Path });
        return Ok(new { ok = true });
    }

    /// <summary>
    /// Refreshes the citizen's WSO2 token pair using the (HttpOnly) refresh-token cookie -- the
    /// only place SevaOne's own backend still needs to actively manage this credential's
    /// lifetime, since the browser can't read/rotate the token itself. No auth required beyond
    /// holding a valid refresh-token cookie: a citizen whose access token has already expired
    /// still needs to be able to call this.
    /// </summary>
    [HttpPost("auth/refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(Wso2CitizenAuthMiddleware.RefreshTokenCookieName, out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new { title = "No refresh token. Please sign in again." });
        }

        var tokens = await _wso2Identity.RefreshAsync(refreshToken, ct);
        if (tokens is null)
        {
            Response.Cookies.Delete(Wso2CitizenAuthMiddleware.AccessTokenCookieName, new CookieOptions { Path = _cookies.Path });
            Response.Cookies.Delete(Wso2CitizenAuthMiddleware.RefreshTokenCookieName, new CookieOptions { Path = _cookies.Path });
            return Unauthorized(new { title = "Session expired. Please sign in again." });
        }

        AppendTokenCookies(tokens);
        return Ok(new { ok = true });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await _auth.GetMeAsync(ct));

    [Authorize]
    [HttpPatch("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest body, CancellationToken ct)
    {
        await ValidateAsync(body, ct);
        return Ok(await _auth.UpdateProfileAsync(body, ct));
    }

    [Authorize]
    [HttpGet("me/digilocker/documents")]
    public async Task<IActionResult> DigiLockerDocuments(CancellationToken ct) =>
        Ok(await _auth.ListDigiLockerDocumentsAsync(ct));

    /// <summary>The dashboard's "Recent Activity" feed. Fixed take, not a query param -- this
    /// is a small recent-history glance, not a paged audit browser.</summary>
    [Authorize]
    [HttpGet("me/audit")]
    public async Task<IActionResult> Audit(CancellationToken ct) => Ok(await _auth.GetMyAuditAsync(20, ct));

    /// <summary>Both cookies are HttpOnly -- the whole point is that no page script can ever read the actual WSO2 token, only present it via the Cookie header the browser attaches automatically.</summary>
    private void AppendTokenCookies(Wso2CitizenTokens tokens)
    {
        var sameSite = Enum.TryParse<SameSiteMode>(_cookies.SameSite, true, out var mode) ? mode : SameSiteMode.Lax;
        Response.Cookies.Append(Wso2CitizenAuthMiddleware.AccessTokenCookieName, tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookies.Secure,
            SameSite = sameSite,
            Path = _cookies.Path,
            Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn)
        });

        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            Response.Cookies.Append(Wso2CitizenAuthMiddleware.RefreshTokenCookieName, tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookies.Secure,
                SameSite = sameSite,
                Path = _cookies.Path,
                // WSO2's refresh-token expiry (app-level refreshToken.expiryInSeconds, currently
                // 86400s) isn't returned on the token response itself -- 24h is that same default,
                // kept in sync manually if that app setting ever changes.
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });
        }
    }

    public sealed record DigiLockerStartBody(string Flow, string? ReturnTo, string? PendingId);
    public sealed record MobileStartBody(string Mobile);
    public sealed record MobileVerifyBody(string ChallengeId, string Otp);
}
