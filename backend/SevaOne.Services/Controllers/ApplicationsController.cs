using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Discovery.Interfaces;
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
    private readonly ITokenAcknowledgementService _acknowledgements;
    private readonly ITokenStatusService _tokenStatus;

    public ApplicationsController(
        IDepartmentSessionService sessions,
        ISubmitApplicationService submit,
        IApplicationStatusService status,
        IPortalApplyService apply,
        IConfiguration config,
        ITokenAcknowledgementService acknowledgements,
        ITokenStatusService tokenStatus)
    {
        _sessions = sessions;
        _submit = submit;
        _status = status;
        _apply = apply;
        _config = config;
        _acknowledgements = acknowledgements;
        _tokenStatus = tokenStatus;
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

    /// <summary>Asks the department for the current status of the signed-in citizen's own application (token
    /// integration, status check) and returns the application as it is afterwards. The department is asked at most
    /// once every few minutes, however often this is called.</summary>
    [Authorize]
    [HttpPost("applications/{reference}/status-refresh")]
    public async Task<IActionResult> RefreshStatus(string reference, CancellationToken ct)
    {
        await _tokenStatus.RefreshForCitizenAsync(reference, ct);
        return Ok(await _status.GetAsync(new GetApplicationRequest(reference), ct));
    }

    /// <summary>The acknowledgement document the department sent for the signed-in citizen's own
    /// application (token integration). Always a download, never rendered in the page.</summary>
    [Authorize]
    [HttpGet("applications/{reference}/acknowledgement")]
    public async Task<IActionResult> Acknowledgement(string reference, CancellationToken ct)
    {
        var file = await _acknowledgements.GetAcknowledgementFileAsync(reference, ct);
        if (file is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }

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
