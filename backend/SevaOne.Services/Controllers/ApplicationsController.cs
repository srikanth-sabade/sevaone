using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Orchestration.DTOs;
using SevaOne.Business.Orchestration.Interfaces;
using SevaOne.Business.Portal.Interfaces;

using SevaOne.Business.Status.DTOs;
using SevaOne.Business.Status.Interfaces;

namespace SevaOne.Services.Controllers;

[Route("api/v1")]
public sealed class ApplicationsController : SevaControllerBase
{
    private readonly IDepartmentSessionService _sessions;
    private readonly ISubmitApplicationService _submit;
    private readonly IApplicationStatusService _status;
    private readonly IPortalApplyService _apply;
    private readonly IConfiguration _config;

    public ApplicationsController(
        IDepartmentSessionService sessions,
        ISubmitApplicationService submit,
        IApplicationStatusService status,
        IPortalApplyService apply,
        IConfiguration config)
    {
        _sessions = sessions;
        _submit = submit;
        _status = status;
        _apply = apply;
        _config = config;
    }

    [Authorize]
    [HttpPost("departments/{code}/sessions")]
    public async Task<IActionResult> OpenDepartmentSession(string code, CancellationToken ct) =>
        Ok(await _sessions.OpenAsync(new OpenDepartmentSessionRequest(code), ct));

    [Authorize]
    [HttpPost("applications")]
    public async Task<IActionResult> Submit([FromBody] SubmitApplicationRequest body, CancellationToken ct)
    {
        await ValidateAsync(body, ct);
        return Ok(await _submit.SubmitAsync(body, ct));
    }

    [Authorize]
    [HttpGet("applications")]
    public async Task<IActionResult> ListMine(CancellationToken ct) => Ok(await _status.ListMineAsync(ct));

    [Authorize]
    [HttpGet("applications/{reference}")]
    public async Task<IActionResult> GetOne(string reference, CancellationToken ct) =>
        Ok(await _status.GetAsync(new GetApplicationRequest(reference), ct));

    [HttpGet("track/{reference}")]
    public async Task<IActionResult> Track(string reference, CancellationToken ct) =>
        Ok(await _status.TrackAsync(new TrackApplicationRequest(reference), ct));

    /// <summary>
    /// Department redirects the citizen's browser here after they finish on the department's
    /// own portal (see PortalDeptFacingService.CompleteApplicationAsync for how the token in
    /// ?token= was minted). Deliberately NOT [Authorize] -- CompleteReturnAsync needs to see
    /// whether a session exists or not (not just reject with a blanket 401) to give the
    /// "your session expired, log back in" case a proper redirect instead of a bare error.
    /// </summary>
    [HttpGet("apply/return")]
    public async Task<IActionResult> ApplyReturn([FromQuery] string token, CancellationToken ct)
    {
        var frontend = (_config["Frontend:PublicOrigin"] ?? "http://localhost:5174").TrimEnd('/');
        var result = await _apply.CompleteReturnAsync(token, ct);
        return Redirect(frontend + result.ReturnTo);
    }
}
