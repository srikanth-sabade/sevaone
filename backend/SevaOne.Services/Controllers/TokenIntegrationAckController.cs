using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Acknowledgement API of the token integration (Seva Sindhu V3, URL 2): the department calls it after it
/// has the application. It sits under /api/v1/dept so DeptGatewayAuthMiddleware applies: the call must come
/// through the WSO2 APIM gateway, which publishes it to departments at /api/TokenIntegration/department-update
/// and is where the per-department IP allow-list and throttling are enforced. Beyond that, the caller must
/// present a Basic login issued from the admin console, and may only acknowledge its own applications.
/// </summary>
[Route("api/v1/dept/token-integration")]
public sealed class TokenIntegrationAckController : SevaControllerBase
{
    private readonly ITokenAcknowledgementService _acknowledgements;
    private readonly ITokenVerificationService _verification;
    private readonly ILogger<TokenIntegrationAckController> _logger;

    public TokenIntegrationAckController(ITokenAcknowledgementService acknowledgements, ITokenVerificationService verification, ILogger<TokenIntegrationAckController> logger)
    {
        _acknowledgements = acknowledgements;
        _verification = verification;
        _logger = logger;
    }

    [RequestSizeLimit(9_000_000)]
    [HttpPost("department-update")]
    public async Task<IActionResult> DepartmentUpdate([FromBody] TokenAckRequest body, CancellationToken ct)
    {
        var caller = await _acknowledgements.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);
        if (caller is null)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"SevaOne token integration\"";
            return Unauthorized(new TokenAckFailure("Unauthorized"));
        }

        var outcome = await _acknowledgements.AcknowledgeAsync(body, caller, ClientIp(), ct);
        if (outcome.IsSuccess) return Ok(outcome.Success);

        // The department only learns that it failed (the contract's reply); the reason is for our logs.
        _logger.LogWarning("Token integration acknowledgement from {Department} refused: {Reason}", caller.DepartmentCode, outcome.Reason);
        return BadRequest(new TokenAckFailure("Data Not Inserted"));
    }

    /// <summary>
    /// The token check: the department sends the token and application reference it received with the citizen and is told whether
    /// they are genuine and not expired, and who the user is. Same login as the acknowledgement. Always answers 200 with
    /// {"valid": true/false, ...}; a problem with the token is in the body, a problem with the login is a 401.
    /// </summary>
    [RequestSizeLimit(20_000)]
    [HttpPost("verify-token")]
    public async Task<IActionResult> VerifyToken([FromBody] TokenVerifyRequest body, CancellationToken ct)
    {
        var caller = await _acknowledgements.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);
        if (caller is null)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"SevaOne token integration\"";
            return Unauthorized(new TokenAckFailure("Unauthorized"));
        }

        Response.Headers.CacheControl = "no-store";
        return Ok(await _verification.VerifyAsync(body, caller, ct));
    }

    /// <summary>The department's address as the APIM gateway saw it (first X-Forwarded-For entry), else the connection's.
    /// Trusted only because the gateway stamp has already been verified for this path.</summary>
    private string? ClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].ToString();
        var first = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return !string.IsNullOrEmpty(first) ? first : HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
