using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Business.Status.DTOs;
using SevaOne.Business.Status.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Department-facing surface. Callers here are department systems, not citizens — there is no
/// "Seva" session cookie on these requests, so these actions are NOT [Authorize]-gated by the
/// citizen auth scheme. Two layers of trust, both required: a WSO2 IS-issued Bearer token
/// (introspected live/cached, see <see cref="IWso2TokenIntrospectionCache"/>) proves WHO is
/// calling — its <c>dept:&lt;code&gt;</c> scope names the department, replacing the old static
/// Basic-auth client list (DeptMachineAuth, removed) with a WSO2 IS <c>client_credentials</c>
/// application per department that can be disabled live, no redeploy — and IDeptTokenGate
/// proves the apply-token itself is genuine and belongs to that department. Body DepartmentId,
/// if supplied, must agree with the authenticated caller's department — it never overrides it.
/// </summary>
[Route("api/v1/dept")]
public sealed class DeptController : SevaControllerBase
{
    private readonly IPortalDeptFacingService _deptFacing;
    private readonly IApplicationStatusService _status;
    private readonly IPortalApplyService _apply;
    private readonly IWso2TokenIntrospectionCache _introspection;
    private readonly ILogger<DeptController> _log;

    public DeptController(
        IPortalDeptFacingService deptFacing,
        IApplicationStatusService status,
        IPortalApplyService apply,
        IWso2TokenIntrospectionCache introspection,
        ILogger<DeptController> log)
    {
        _deptFacing = deptFacing;
        _status = status;
        _apply = apply;
        _introspection = introspection;
        _log = log;
    }

    [HttpPost("user-details")]
    public async Task<IActionResult> GetUserDetails([FromBody] UserDetailsBody body, CancellationToken ct)
    {
        var (ok, departmentId, authFailure) = await TryResolveDepartmentAsync(body.DepartmentId, "Get User Details", ct);
        if (!ok)
        {
            return authFailure!;
        }

        var request = new GetUserDetailsRequest(departmentId!, body.Token);
        await ValidateAsync(request, ct);
        return Ok(await _deptFacing.GetUserDetailsAsync(request, ct));
    }

    /// <summary>
    /// Integration testing only. Mints an apply-token for a fixed seeded test citizen -- no real
    /// citizen or operator involved, no browser handoff. Lets a department self-serve a fresh
    /// Token to drive their own User Details / Get Acknowledgement / Payment Status testing,
    /// without waiting on someone at SevaOne to trigger a real apply flow. Same three-layer trust
    /// as every other /dept/* call (ApiKey checked by APIM, DeptMachineAuth checked here) -- no
    /// separate credential. The response shape is the same ApplyTokenDto a real apply mints:
    /// `applyUrl` carries the encrypted Token as its `Token=` query parameter, exactly like
    /// Section 2 of the integration guide already teaches departments to extract.
    /// </summary>
    [HttpPost("test-token")]
    public async Task<IActionResult> TestToken([FromBody] TestTokenBody body, CancellationToken ct)
    {
        var (ok, departmentId, authFailure) = await TryResolveDepartmentAsync(body.DepartmentId, "Test Token", ct);
        if (!ok)
        {
            return authFailure!;
        }

        var request = new CreateTestApplyTokenRequest(departmentId!, body.ServiceId);
        await ValidateAsync(request, ct);
        return Ok(await _apply.CreateTestApplyTokenAsync(request, ct));
    }

    [HttpPost("payment-status")]
    public async Task<IActionResult> PaymentStatus([FromBody] PaymentStatusBody body, CancellationToken ct)
    {
        var (ok, departmentId, authFailure) = await TryResolveDepartmentAsync(body.DepartmentId, "Payment Status POST", ct);
        if (!ok)
        {
            return authFailure!;
        }

        var request = new PostPaymentStatusRequest(departmentId!, body.Token, body.PaymentStatus, body.Amount, body.TransactionId);
        await ValidateAsync(request, ct);
        return Ok(await _deptFacing.PostPaymentStatusAsync(request, ct));
    }

    /// <summary>Department -> SevaOne: application finished, get a one-time token to redirect the citizen's browser back with.</summary>
    [HttpPost("get-acknowledgement")]
    public async Task<IActionResult> CompleteApplication([FromBody] CompleteApplicationBody body, CancellationToken ct)
    {
        var (ok, departmentId, authFailure) = await TryResolveDepartmentAsync(body.DepartmentId, "Get Acknowledgement", ct);
        if (!ok)
        {
            return authFailure!;
        }

        var request = new CompleteApplicationRequest(departmentId!, body.SevaOneUserId, body.ServiceId, body.Status, body.AcknowledgementId);
        await ValidateAsync(request, ct);
        return Ok(await _deptFacing.CompleteApplicationAsync(request, ct));
    }

    /// <summary>
    /// Department -> SevaOne: ongoing status change for an application already completed via
    /// get-acknowledgement, keyed on the department's own acknowledgement id, not a token (there
    /// isn't one -- this can be called any number of times, long after the citizen's browser
    /// session ended). Replaces the old /api/v1/internal/webhooks/status endpoint, which used a
    /// single static shared secret instead of the same three-layer trust model every other
    /// /dept/* call gets.
    /// </summary>
    [HttpPost("status-update")]
    public async Task<IActionResult> StatusUpdate([FromBody] StatusUpdateBody body, CancellationToken ct)
    {
        var (ok, departmentId, authFailure) = await TryResolveDepartmentAsync(body.DepartmentId, "Status Update", ct);
        if (!ok)
        {
            return authFailure!;
        }

        var request = new ApplyDepartmentStatusRequest(departmentId!, body.AcknowledgementId, body.Status, body.Note);
        await ValidateAsync(request, ct);
        var changed = await _status.ApplyDepartmentStatusAsync(request, ct);
        return Ok(new { accepted = true, changed });
    }

    /// <summary>
    /// Authenticates the caller via its WSO2 IS-issued Bearer token and resolves which
    /// department it's allowed to act as, from that token's <c>dept:&lt;code&gt;</c> scope.
    /// Body DepartmentId, if present, must match — it's a cross-check, not a source of truth.
    /// Disabling a department's WSO2 IS application is a live admin action: its next
    /// <c>client_credentials</c> call fails immediately, and any already-issued token stops
    /// introspecting as active within the cache's bounded window (see
    /// <see cref="IWso2TokenIntrospectionCache"/>) — no redeploy needed either way.
    /// </summary>
    private async Task<(bool Ok, string? DepartmentId, IActionResult? Failure)> TryResolveDepartmentAsync(
        string? bodyDepartmentId, string action, CancellationToken ct)
    {
        var auth = Request.Headers.Authorization.ToString();
        string? bearer = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? auth["Bearer ".Length..].Trim()
            : null;

        if (string.IsNullOrWhiteSpace(bearer))
        {
            _log.LogWarning("[TOKEN:MACHINE:REJECT] {Action} — missing_bearer", action);
            var missing = StatusCode(StatusCodes.Status401Unauthorized, new
            {
                code = SevaErrorCodes.Auth.MissingCredentials,
                title = "Department Bearer token missing or invalid."
            });
            return (false, null, missing);
        }

        var result = await _introspection.ValidateAsync(bearer, ct);
        var deptFromToken = ParseScope(result.Scope, "dept:");
        if (!result.Active || deptFromToken is null)
        {
            _log.LogWarning("[TOKEN:MACHINE:REJECT] {Action} — active={Active}, scope={Scope}", action, result.Active, result.Scope);
            var invalid = StatusCode(StatusCodes.Status401Unauthorized, new
            {
                code = SevaErrorCodes.Auth.InvalidCredentials,
                title = "Department Bearer token missing or invalid."
            });
            return (false, null, invalid);
        }

        if (!string.IsNullOrWhiteSpace(bodyDepartmentId)
            && !string.Equals(bodyDepartmentId, deptFromToken, StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("[TOKEN:MACHINE:REJECT] {Action} — body department {Body} != authenticated caller's {Caller}", action, bodyDepartmentId, deptFromToken);
            var mismatch = StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = SevaErrorCodes.Auth.DepartmentMismatch,
                title = "Request body's departmentId does not match the authenticated caller's department."
            });
            return (false, null, mismatch);
        }

        return (true, deptFromToken, null);
    }

    /// <summary>Reads a single <c>prefix:value</c> entry out of a space-separated OAuth2 scope string (e.g. "dept:revenue svc:SVC001").</summary>
    private static string? ParseScope(string? scope, string prefix) =>
        scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(s => s.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];

    public sealed record UserDetailsBody(string? DepartmentId, string Token);
    public sealed record TestTokenBody(string? DepartmentId, string ServiceId);
    public sealed record PaymentStatusBody(string? DepartmentId, string Token, string PaymentStatus, decimal? Amount, string? TransactionId);
    public sealed record CompleteApplicationBody(string? DepartmentId, long SevaOneUserId, string ServiceId, string Status, string? AcknowledgementId);
    public sealed record StatusUpdateBody(string? DepartmentId, string AcknowledgementId, string Status, string Note);
}
