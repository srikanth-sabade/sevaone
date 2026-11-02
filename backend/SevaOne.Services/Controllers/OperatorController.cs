using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Services.Auth;
using SevaOne.Services.Middleware;

namespace SevaOne.Services.Controllers;

/// <summary>Operator (kiosk/counter) login and assisted-mode apply-token minting -- separate identity/session from the citizen-facing AuthController.</summary>
[Route("api/v1/operator")]
public sealed class OperatorController : SevaControllerBase
{
    private readonly IOperatorAuthService _auth;
    private readonly IOperatorClaimService _claim;
    private readonly IPortalApplyService _apply;
    private readonly IOperatorCitizenBindService _bind;
    private readonly CookieSettings _cookies;

    public OperatorController(
        IOperatorAuthService auth,
        IOperatorClaimService claim,
        IPortalApplyService apply,
        IOperatorCitizenBindService bind,
        IOptions<CookieSettings> cookies)
    {
        _auth = auth;
        _claim = claim;
        _apply = apply;
        _bind = bind;
        _cookies = cookies.Value;
    }

    /// <summary>First step of the self-claim flow -- see OperatorClaimService's doc comment.
    /// No [Authorize]: the whole point is an operator has no session yet.</summary>
    [EnableRateLimiting("auth-start")]
    [HttpPost("claim/start")]
    public async Task<IActionResult> ClaimStart([FromBody] OperatorClaimStartRequest body, CancellationToken ct)
    {
        var result = await _claim.StartAsync(body, ct);
        return Ok(result);
    }

    [EnableRateLimiting("auth-credential")]
    [HttpPost("claim/verify")]
    public async Task<IActionResult> ClaimVerify([FromBody] OperatorClaimVerifyRequest body, CancellationToken ct)
    {
        var result = await _claim.VerifyAsync(body, ct);
        return Ok(result);
    }

    [EnableRateLimiting("auth-credential")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] OperatorLoginBody body, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(new OperatorLoginRequest(body.OperatorCode, body.Password), ct);
        AppendSessionCookie(result.SessionId, result.Ttl);
        return Ok(new { operatorName = result.OperatorName });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(OperatorSessionMiddleware.CookieName, out var sid);
        await _auth.LogoutAsync(new OperatorLogoutRequest(sid), ct);
        Response.Cookies.Delete(OperatorSessionMiddleware.CookieName);
        return Ok(new { ok = true });
    }

    [Authorize(AuthenticationSchemes = "SevaOperator")]
    [HttpPost("apply-token")]
    public async Task<IActionResult> AssistedApplyToken([FromBody] AssistedApplyTokenBody body, CancellationToken ct)
    {
        var result = await _apply.CreateAssistedApplyTokenAsync(
            new CreateAssistedApplyTokenRequest(body.DepartmentCode, body.ServiceCode, body.SevaOneUserId), ct);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = "SevaOperator")]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _auth.GetMeAsync(ct);
        return Ok(result);
    }

    [EnableRateLimiting("auth-credential")]
    [Authorize(AuthenticationSchemes = "SevaOperator")]
    [HttpPost("citizens/otp")]
    public async Task<IActionResult> SendCitizenOtp([FromBody] SendCitizenOtpBody body, CancellationToken ct)
    {
        var result = await _bind.SendOtpAsync(new SendOperatorCitizenOtpRequest(body.Mobile), ct);
        return Ok(result);
    }

    [EnableRateLimiting("auth-credential")]
    [Authorize(AuthenticationSchemes = "SevaOperator")]
    [HttpPost("citizens/verify")]
    public async Task<IActionResult> VerifyCitizenOtp([FromBody] VerifyCitizenOtpBody body, CancellationToken ct)
    {
        var result = await _bind.VerifyAndBindAsync(new VerifyOperatorCitizenOtpRequest(body.ChallengeId, body.Otp), ct);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = "SevaOperator")]
    [HttpGet("citizens/bound")]
    public async Task<IActionResult> BoundCitizen(CancellationToken ct)
    {
        var result = await _bind.GetBoundCitizenAsync(ct);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = "SevaOperator")]
    [HttpPost("citizens/unbind")]
    public async Task<IActionResult> Unbind(CancellationToken ct)
    {
        await _bind.UnbindAsync(ct);
        return Ok(new { ok = true });
    }

    private void AppendSessionCookie(string sessionId, TimeSpan ttl)
    {
        var sameSite = Enum.TryParse<SameSiteMode>(_cookies.SameSite, true, out var mode) ? mode : SameSiteMode.Lax;
        Response.Cookies.Append(OperatorSessionMiddleware.CookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookies.Secure,
            SameSite = sameSite,
            Path = _cookies.Path,
            Expires = DateTimeOffset.UtcNow.Add(ttl)
        });
    }

    public sealed record OperatorLoginBody(string OperatorCode, string Password);
    public sealed record AssistedApplyTokenBody(string DepartmentCode, string ServiceCode, long SevaOneUserId);
    public sealed record SendCitizenOtpBody(string Mobile);
    public sealed record VerifyCitizenOtpBody(string ChallengeId, string Otp);
}
