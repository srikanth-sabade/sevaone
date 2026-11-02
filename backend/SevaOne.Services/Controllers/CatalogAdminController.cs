using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Services.Auth;
using SevaOne.Services.Middleware;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Catalog admin login, and ingestion: upload -> stage -> review -> publish. Every action
/// except Login/Logout requires the "SevaAdmin" authentication scheme (AdminSessionMiddleware
/// + AdminAuthHandler, mirroring the citizen/operator pattern) -- attribute is per-action, not
/// class-level, so login itself isn't gated behind the very session it creates.
/// </summary>
[Route("api/v1/admin/catalog")]
public sealed class CatalogAdminController : SevaControllerBase
{
    private readonly ICatalogAdminAuthService _auth;
    private readonly ICatalogIngestionService _ingestion;
    private readonly ICatalogPublishService _publish;
    private readonly ICatalogVisibilityService _visibility;
    private readonly CookieSettings _cookies;

    public CatalogAdminController(
        ICatalogAdminAuthService auth, ICatalogIngestionService ingestion, ICatalogPublishService publish,
        ICatalogVisibilityService visibility, IOptions<CookieSettings> cookies)
    {
        _auth = auth;
        _ingestion = ingestion;
        _publish = publish;
        _visibility = visibility;
        _cookies = cookies.Value;
    }

    [EnableRateLimiting("auth-credential")]
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginBody body, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(new CatalogAdminLoginRequest(body.AdminCode, body.Password), ct);
        AppendSessionCookie(result.SessionId, result.Ttl);
        return Ok(new { adminName = result.AdminName });
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(AdminSessionMiddleware.CookieName, out var sid);
        await _auth.LogoutAsync(new CatalogAdminLogoutRequest(sid), ct);
        Response.Cookies.Delete(AdminSessionMiddleware.CookieName);
        return Ok(new { ok = true });
    }

    /// <summary>Session probe for the admin console -- mirrors AuthController's citizen-facing GET /me. [Authorize] alone (not a hand-rolled cookie check) gives a real 401 when there's no valid SevaAdmin session, which is what the frontend's useQuery(retry:false) session-probe pattern expects.</summary>
    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpGet("auth/me")]
    public IActionResult Me() => Ok(new { adminName = CurrentAdminName() });

    /// <summary>The JSON path -- for systems (a future integration, or this session's own 754-service migration), not the human upload flow.</summary>
    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpPost("imports")]
    public async Task<IActionResult> StartImport([FromBody] StartImportRequest request, CancellationToken ct)
    {
        var uploadedBy = CurrentAdminName();
        var batchId = await _ingestion.StartImportAsync(request.SourceFilename, "json", uploadedBy, request.Rows, ct);
        return Ok(new { batchId });
    }

    /// <summary>The human upload flow -- drag-drop a file in the admin console. Only .json is actually parsed today; .xlsx/.csv return 400 rather than silently mis-parsing or pretending to succeed. A real spreadsheet parser is follow-up work.</summary>
    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpPost("imports/file")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> StartImportFromFile(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(new { title = "The uploaded file is empty." });

        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        if (extension != "json")
        {
            return BadRequest(new { title = $"\"{extension}\" isn't supported yet -- only .json is parsed today. Convert to the canonical row shape and upload that, or use POST /imports directly." });
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var json = await reader.ReadToEndAsync(ct);
        var rows = System.Text.Json.JsonSerializer.Deserialize<List<CatalogRow>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        var uploadedBy = CurrentAdminName();
        var batchId = await _ingestion.StartImportAsync(file.FileName, "json", uploadedBy, rows, ct);
        return Ok(new { batchId });
    }

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpGet("imports/{batchId:guid}")]
    public async Task<IActionResult> GetBatch(Guid batchId, CancellationToken ct)
    {
        var batch = await _ingestion.GetBatchAsync(batchId, ct);
        return batch is null ? NotFound() : Ok(batch);
    }

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpGet("imports/{batchId:guid}/rows")]
    public async Task<IActionResult> ListRows(Guid batchId, string? action, CancellationToken ct) =>
        Ok(await _ingestion.ListRowsAsync(batchId, action, ct));

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpPost("imports/{batchId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid batchId, CancellationToken ct)
    {
        var publishedBy = CurrentAdminName();
        var result = await _publish.PublishAsync(batchId, publishedBy, ct);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpPost("imports/{batchId:guid}/discard")]
    public async Task<IActionResult> Discard(Guid batchId, CancellationToken ct)
    {
        await _ingestion.DiscardAsync(batchId, ct);
        return Ok();
    }

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken ct) => Ok(await _visibility.ListRolesAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpGet("services/{departmentCode}/{serviceCode}/visibility")]
    public async Task<IActionResult> GetServiceVisibility(string departmentCode, string serviceCode, CancellationToken ct) =>
        Ok(await _visibility.GetServiceVisibilityAsync(departmentCode, serviceCode, ct));

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpPut("services/{departmentCode}/{serviceCode}/visibility/{roleKey}")]
    public async Task<IActionResult> SetServiceVisibility(string departmentCode, string serviceCode, string roleKey, [FromBody] SetVisibilityBody body, CancellationToken ct)
    {
        var changedBy = CurrentAdminName();
        await _visibility.SetServiceVisibilityAsync(departmentCode, serviceCode, roleKey, body.Grant, changedBy, ct);
        return Ok();
    }

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpPost("services/visibility/bulk")]
    public async Task<IActionResult> BulkSetVisibility([FromBody] BulkVisibilityRequest request, CancellationToken ct)
    {
        var changedBy = CurrentAdminName();
        var result = await _visibility.BulkSetVisibilityAsync(request, changedBy, ct);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = "SevaAdmin")]
    [HttpGet("change-log")]
    public async Task<IActionResult> ListChangeLog(string? departmentCode, string? serviceCode, int limit = 50, CancellationToken ct = default) =>
        Ok(await _visibility.ListChangeLogAsync(departmentCode, serviceCode, limit, ct));

    /// <summary>User.Identity?.Name resolves to whichever identity is first on the principal -- not reliably the "SevaAdmin" one once a citizen and/or operator identity may also be sitting on the same ClaimsPrincipal (they're independent, coexisting sessions, see Wso2CitizenAuthMiddleware/OperatorSessionMiddleware). Look up the SevaAdmin identity's Name claim explicitly instead.</summary>
    private string CurrentAdminName() =>
        User.Identities.FirstOrDefault(i => i.AuthenticationType == "SevaAdmin")?.Name ?? "unknown-admin";

    private void AppendSessionCookie(string sessionId, TimeSpan ttl)
    {
        var sameSite = Enum.TryParse<SameSiteMode>(_cookies.SameSite, true, out var mode) ? mode : SameSiteMode.Lax;
        Response.Cookies.Append(AdminSessionMiddleware.CookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookies.Secure,
            SameSite = sameSite,
            Path = _cookies.Path,
            Expires = DateTimeOffset.UtcNow.Add(ttl)
        });
    }

    public sealed record AdminLoginBody(string AdminCode, string Password);
    public sealed record SetVisibilityBody(bool Grant);
}

public sealed record StartImportRequest(string SourceFilename, IReadOnlyList<CatalogRow> Rows);
