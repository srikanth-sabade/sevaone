using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Practice and test endpoints for the Department Guide, the standalone site departments use to build their side of the token
/// integration. Every call needs the department's integration login (Basic), the same one used for the acknowledgement API, so a
/// test only ever acts for the department that is logged in. Nothing here touches a real citizen or a real application.
/// </summary>
[Route("api/v1/dept-portal")]
[EnableRateLimiting("dept-portal")]
public sealed class DeptPortalController : SevaControllerBase
{
    private readonly ITokenAcknowledgementService _acknowledgements;
    private readonly IDeptPortalService _portal;

    public DeptPortalController(ITokenAcknowledgementService acknowledgements, IDeptPortalService portal)
    {
        _acknowledgements = acknowledgements;
        _portal = portal;
    }

    /// <summary>Checks the login and says which department it is. The guide uses it for "sign in".</summary>
    [HttpPost("whoami")]
    public async Task<IActionResult> WhoAmI(CancellationToken ct)
    {
        var caller = await AuthenticateAsync(ct);
        return caller is null ? Denied() : Ok(new { departmentCode = caller.DepartmentCode });
    }

    /// <summary>A practice application: the fields SevaOne would post to your landing page, with a real, working test token.</summary>
    [HttpPost("sandbox/start")]
    public async Task<IActionResult> StartSandbox([FromBody] SandboxStartRequest? body, CancellationToken ct)
    {
        var caller = await AuthenticateAsync(ct);
        if (caller is null) return Denied();
        return Ok(await _portal.StartSandboxAsync(caller, body?.ServiceCode, ct));
    }

    [HttpPost("sandbox/verify-token")]
    public async Task<IActionResult> VerifySandbox([FromBody] TokenVerifyRequest body, CancellationToken ct)
    {
        var caller = await AuthenticateAsync(ct);
        if (caller is null) return Denied();
        return Ok(await _portal.VerifySandboxAsync(body, caller, ct));
    }

    /// <summary>Same as the real acknowledgement API, for practice applications only. A refusal also says why, which the real API does not.</summary>
    [RequestSizeLimit(9_000_000)]
    [HttpPost("sandbox/acknowledgement")]
    public async Task<IActionResult> AcknowledgeSandbox([FromBody] TokenAckRequest body, CancellationToken ct)
    {
        var caller = await AuthenticateAsync(ct);
        if (caller is null) return Denied();

        var (outcome, notSandbox) = await _portal.AcknowledgeSandboxAsync(body, caller, ClientIp(), ct);
        if (outcome.IsSuccess) return Ok(outcome.Success);
        return BadRequest(new SandboxAckFailure("Data Not Inserted", notSandbox ? "This call is for practice applications (EDCSTEST…) only." : outcome.Reason));
    }

    /// <summary>SevaOne posts a practice hand-off to your landing page and tells you what it answered.</summary>
    [HttpPost("test/landing")]
    public async Task<IActionResult> TestLanding([FromBody] LandingTestRequest body, CancellationToken ct)
    {
        var caller = await AuthenticateAsync(ct);
        if (caller is null) return Denied();
        return Ok(await _portal.TestLandingAsync(caller, body.Url ?? "", body.ServiceCode, ct));
    }

    /// <summary>SevaOne calls your status API with a number you choose and shows what it found in the answer. Nothing is saved.</summary>
    [HttpPost("test/status")]
    public async Task<IActionResult> TestStatus([FromBody] StatusTestRequest body, CancellationToken ct)
    {
        var caller = await AuthenticateAsync(ct);
        if (caller is null) return Denied();
        return Ok(await _portal.TestStatusAsync(body, ct));
    }

    private Task<TokenIntegrationCaller?> AuthenticateAsync(CancellationToken ct) =>
        _acknowledgements.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);

    private IActionResult Denied()
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"SevaOne department guide\"";
        return Unauthorized(new TokenAckFailure("Unauthorized"));
    }

    private string? ClientIp()
    {
        var first = Request.Headers["X-Forwarded-For"].ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return !string.IsNullOrEmpty(first) ? first : HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
