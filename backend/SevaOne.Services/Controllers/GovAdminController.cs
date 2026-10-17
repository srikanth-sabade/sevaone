using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Reference;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.AiBackend;
using SevaOne.Services.Auth;
using SevaOne.Services.Middleware;

namespace SevaOne.Services.Controllers;

/// <summary>Government admin panel login, first-time-login claim, admin-management, and
/// read-only analytics -- separate identity/session from every other persona (citizen
/// AuthController, OperatorController, CatalogAdminController). Analytics endpoints resolve
/// district/taluk/department scoping only from ICurrentGovAdmin (via IGovDashboardService) --
/// there is deliberately no such query parameter anywhere on this controller, so a client can
/// never widen its own scope.</summary>
[Route("api/v1/admin/gov")]
public sealed class GovAdminController : SevaControllerBase
{
    private readonly IGovAdminAuthService _auth;
    private readonly IGovAdminClaimService _claim;
    private readonly IGovAdminManagementService _management;
    private readonly IOperatorManagementService _operators;
    private readonly IGovAdminBulkImportService _adminImports;
    private readonly IGovAdminActivityLogService _activityLog;
    private readonly IServiceNoticeService _serviceNotice;
    private readonly IGovNotificationService _govNotifications;
    private readonly IGovDashboardService _dashboard;
    private readonly ICatalogManagementService _catalogManagement;
    private readonly ICatalogIngestionService _ingestion;
    private readonly ICatalogPublishService _publish;
    private readonly IAiIngestionClient _aiIngestion;
    private readonly IGovAdminAiAssistantClient _aiAssistant;
    private readonly IAiMonitorClient _aiMonitor;
    private readonly IDiscoveryRepository _discovery;
    private readonly CookieSettings _cookies;

    public GovAdminController(
        IGovAdminAuthService auth,
        IGovAdminClaimService claim,
        IGovAdminManagementService management,
        IOperatorManagementService operators,
        IGovAdminBulkImportService adminImports,
        IGovAdminActivityLogService activityLog,
        IServiceNoticeService serviceNotice,
        IGovNotificationService govNotifications,
        IGovDashboardService dashboard,
        ICatalogManagementService catalogManagement,
        ICatalogIngestionService ingestion,
        ICatalogPublishService publish,
        IAiIngestionClient aiIngestion,
        IGovAdminAiAssistantClient aiAssistant,
        IAiMonitorClient aiMonitor,
        IDiscoveryRepository discovery,
        IOptions<CookieSettings> cookies)
    {
        _auth = auth;
        _claim = claim;
        _management = management;
        _operators = operators;
        _adminImports = adminImports;
        _activityLog = activityLog;
        _serviceNotice = serviceNotice;
        _govNotifications = govNotifications;
        _dashboard = dashboard;
        _catalogManagement = catalogManagement;
        _ingestion = ingestion;
        _publish = publish;
        _aiIngestion = aiIngestion;
        _aiAssistant = aiAssistant;
        _aiMonitor = aiMonitor;
        _discovery = discovery;
        _cookies = cookies.Value;
    }

    [EnableRateLimiting("auth-credential")]
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] GovAdminLoginBody body, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(new GovAdminLoginRequest(body.Username, body.Password), ct);
        AppendSessionCookie(result.SessionId, result.Ttl);
        return Ok(new { displayName = result.DisplayName, tier = result.Tier });
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(GovAdminSessionMiddleware.CookieName, out var sid);
        await _auth.LogoutAsync(new GovAdminLogoutRequest(sid), ct);
        Response.Cookies.Delete(GovAdminSessionMiddleware.CookieName);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("auth/me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _auth.GetMeAsync(ct);
        return Ok(result);
    }

    /// <summary>First-time-login claim, step 1: username the Superadmin assigned + the new
    /// admin's own mobile + chosen password -> OTP sent. No [Authorize]: the whole point is a
    /// newly-created admin has no session yet.</summary>
    [EnableRateLimiting("auth-start")]
    [HttpPost("claim/start")]
    public async Task<IActionResult> ClaimStart([FromBody] GovAdminClaimStartRequest body, CancellationToken ct)
    {
        var result = await _claim.StartAsync(body, ct);
        return Ok(result);
    }

    [EnableRateLimiting("auth-credential")]
    [HttpPost("claim/verify")]
    public async Task<IActionResult> ClaimVerify([FromBody] GovAdminClaimVerifyRequest body, CancellationToken ct)
    {
        var result = await _claim.VerifyAsync(body, ct);
        return Ok(result);
    }

    /// <summary>Superadmin-only: create a new gov-admin account (username + role + scope). The
    /// new admin sets their own password via claim/start+verify above -- Superadmin never
    /// handles a password. IGovAdminManagementService enforces the Superadmin-only check itself.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/admins")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateGovAdminRequest body, CancellationToken ct)
    {
        var result = await _management.CreateAsync(body, ct);
        return Ok(result);
    }

    /// <summary>Superadmin (full detail) and ChiefMinister (view-only, per the role spec) can
    /// both list every gov-admin account. IGovAdminManagementService enforces which tiers may
    /// call this.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/admins")]
    public async Task<IActionResult> ListAdmins(CancellationToken ct) => Ok(await _management.ListAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/admins/{id:guid}/active")]
    public async Task<IActionResult> SetAdminActive(Guid id, [FromBody] SetGovAdminActiveBody body, CancellationToken ct)
    {
        await _management.SetActiveAsync(id, body.IsActive, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/admins/{id:guid}/sessions")]
    public async Task<IActionResult> ListAdminSessions(Guid id, CancellationToken ct) => Ok(await _management.ListSessionsAsync(id, ct));

    /// <summary>Superadmin/ChiefMinister only (same tier check as ListAdmins above) -- who did
    /// what, across logins, admin account changes, and notice/notification edits. Backed by
    /// portal.gov_admin_activity_log, a dedicated table (see V73), not audit.audit_logs (which
    /// is citizen-scoped only).</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/activity-log")]
    public async Task<IActionResult> ListActivityLog(int limit = 100, string? action = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default) =>
        Ok(await _activityLog.ListAsync(limit, action, from, to, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpDelete("management/admins/{id:guid}/sessions/{sessionId}")]
    public async Task<IActionResult> ForceLogoutAdminSession(Guid id, string sessionId, CancellationToken ct)
    {
        await _management.ForceLogoutAsync(id, sessionId, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/operators")]
    public async Task<IActionResult> ListOperators(string? search, bool? isActive, string? operatorType, CancellationToken ct) =>
        Ok(await _operators.ListAsync(search, isActive, operatorType, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/operators/{id:guid}/active")]
    public async Task<IActionResult> SetOperatorActive(Guid id, [FromBody] SetGovAdminActiveBody body, CancellationToken ct)
    {
        await _operators.SetActiveAsync(id, body.IsActive, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/admins/imports")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> StartGovAdminImport([FromBody] StartGovAdminImportRequest body, CancellationToken ct)
    {
        var format = body.SourceFormat.Trim().ToLowerInvariant();
        if (format is not ("csv" or "xlsx")) return BadRequest(new { title = "Source format must be csv or xlsx." });
        var batchId = await _adminImports.StartImportAsync(body.SourceFilename, format, body.Rows, ct);
        return Ok(new { batchId });
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/admins/imports/{batchId:guid}")]
    public async Task<IActionResult> GetGovAdminImport(Guid batchId, CancellationToken ct)
    {
        var batch = await _adminImports.GetBatchAsync(batchId, ct);
        return batch is null ? NotFound() : Ok(batch);
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/admins/imports/{batchId:guid}/rows")]
    public async Task<IActionResult> GetGovAdminImportRows(Guid batchId, CancellationToken ct) => Ok(await _adminImports.ListRowsAsync(batchId, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/admins/imports/{batchId:guid}/publish")]
    public async Task<IActionResult> PublishGovAdminImport(Guid batchId, CancellationToken ct) => Ok(await _adminImports.PublishAsync(batchId, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/admins/imports/{batchId:guid}/discard")]
    public async Task<IActionResult> DiscardGovAdminImport(Guid batchId, CancellationToken ct)
    {
        await _adminImports.DiscardAsync(batchId, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/service-notice")]
    public async Task<IActionResult> GetServiceNotice(CancellationToken ct) => new JsonResult(await _serviceNotice.GetForAdminAsync(ct))
    {
        StatusCode = StatusCodes.Status200OK
    };

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPut("management/service-notice")]
    public async Task<IActionResult> SaveServiceNotice([FromBody] SaveServiceNoticeBody body, CancellationToken ct)
    {
        var result = await _serviceNotice.SaveAsync(
            new SaveServiceNoticeRequest(body.Title, body.Message, body.Severity, body.IsActive, body.StartsAt, body.EndsAt), ct);
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/service-notice/history")]
    public async Task<IActionResult> GetServiceNoticeHistory(CancellationToken ct)
    {
        return Ok(await _serviceNotice.GetHistoryAsync(ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpDelete("management/service-notice")]
    public async Task<IActionResult> DeleteServiceNotice(CancellationToken ct)
    {
        await _serviceNotice.StopAsync(ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/notifications")]
    public async Task<IActionResult> ListGovNotifications(CancellationToken ct)
    {
        return Ok(await _govNotifications.ListForAdminAsync(ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/notifications")]
    public async Task<IActionResult> CreateGovNotification([FromBody] CreateGovNotificationBody body, CancellationToken ct)
    {
        return Ok(await _govNotifications.CreateAsync(new CreateGovNotificationRequest(body.Title, body.Body), ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/notifications/{id:guid}/active")]
    public async Task<IActionResult> SetGovNotificationActive(Guid id, [FromBody] SetGovNotificationActiveBody body, CancellationToken ct)
    {
        await _govNotifications.SetActiveAsync(id, body.IsActive, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/catalog/departments")]
    public async Task<IActionResult> ListCatalogDepartments(CancellationToken ct) => Ok(await _catalogManagement.ListDepartmentsAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/catalog/departments")]
    public async Task<IActionResult> CreateCatalogDepartment([FromBody] CatalogAdminDepartmentWriteRequest body, CancellationToken ct) =>
        Ok(await _catalogManagement.CreateDepartmentAsync(body, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/catalog/departments/{code}")]
    public async Task<IActionResult> UpdateCatalogDepartment(string code, [FromBody] CatalogAdminDepartmentWriteRequest body, CancellationToken ct) =>
        Ok(await _catalogManagement.UpdateDepartmentAsync(code, body, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/catalog/departments/{code}/active")]
    public async Task<IActionResult> SetCatalogDepartmentActive(string code, [FromBody] SetCatalogActiveBody body, CancellationToken ct)
    {
        await _catalogManagement.SetDepartmentActiveAsync(code, body.IsActive, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/catalog/sub-departments")]
    public async Task<IActionResult> ListCatalogSubDepartments(string? departmentCode, string? search, bool? isActive, CancellationToken ct)
    {
        RequireCatalogSuperadmin();
        return Ok(await _catalogManagement.ListSubDepartmentsAsync(departmentCode, search, isActive, ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/catalog/sub-departments")]
    public async Task<IActionResult> CreateCatalogSubDepartment([FromBody] CatalogAdminSubDepartmentWriteRequest body, CancellationToken ct) =>
        Ok(await _catalogManagement.CreateSubDepartmentAsync(body, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/catalog/sub-departments/{id:guid}")]
    public async Task<IActionResult> UpdateCatalogSubDepartment(Guid id, [FromBody] CatalogAdminSubDepartmentWriteRequest body, CancellationToken ct) =>
        Ok(await _catalogManagement.UpdateSubDepartmentAsync(id, body, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/catalog/sub-departments/{id:guid}/active")]
    public async Task<IActionResult> SetCatalogSubDepartmentActive(Guid id, [FromBody] SetCatalogActiveBody body, CancellationToken ct)
    {
        await _catalogManagement.SetSubDepartmentActiveAsync(id, body.IsActive, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/catalog/services")]
    public async Task<IActionResult> ListCatalogServices(string? departmentCode, string? search, bool? isActive, CancellationToken ct) =>
        Ok(await _catalogManagement.ListServicesAsync(departmentCode, search, isActive, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/catalog/services/{departmentCode}/{serviceCode}")]
    public async Task<IActionResult> GetCatalogService(string departmentCode, string serviceCode, CancellationToken ct) =>
        Ok(await _catalogManagement.GetServiceAsync(departmentCode, serviceCode, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/catalog/services")]
    public async Task<IActionResult> CreateCatalogService([FromBody] CatalogAdminServiceWriteRequest body, CancellationToken ct) =>
        Ok(await _catalogManagement.CreateServiceAsync(body, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/catalog/services/{departmentCode}/{serviceCode}")]
    public async Task<IActionResult> UpdateCatalogService(string departmentCode, string serviceCode, [FromBody] CatalogAdminServiceWriteRequest body, CancellationToken ct) =>
        Ok(await _catalogManagement.UpdateServiceAsync(departmentCode, serviceCode, body, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPatch("management/catalog/services/{departmentCode}/{serviceCode}/active")]
    public async Task<IActionResult> SetCatalogServiceActive(string departmentCode, string serviceCode, [FromBody] SetCatalogActiveBody body, CancellationToken ct)
    {
        await _catalogManagement.SetServiceActiveAsync(departmentCode, serviceCode, body.IsActive, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/catalog/changes")]
    public async Task<IActionResult> ListCatalogChanges(int limit = 100, CancellationToken ct = default) =>
        Ok(await _catalogManagement.ListChangesAsync(limit, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/catalog/imports")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> StartCatalogImport([FromBody] StartCatalogImportRequest body, CancellationToken ct)
    {
        RequireCatalogSuperadmin();
        var sourceFormat = body.SourceFormat.Trim().ToLowerInvariant();
        if (sourceFormat is not ("csv" or "xlsx"))
        {
            return BadRequest(new { title = "Source format must be csv or xlsx." });
        }

        var batchId = await _ingestion.StartImportAsync(body.SourceFilename, sourceFormat, CurrentGovAdminName(), body.Rows, ct);
        return Ok(new { batchId });
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/catalog/imports/{batchId:guid}")]
    public async Task<IActionResult> GetCatalogImport(Guid batchId, CancellationToken ct)
    {
        RequireCatalogSuperadmin();
        var batch = await _ingestion.GetBatchAsync(batchId, ct);
        return batch is null ? NotFound() : Ok(batch);
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/catalog/imports/{batchId:guid}/rows")]
    public async Task<IActionResult> GetCatalogImportRows(Guid batchId, string? action, CancellationToken ct) =>
        await GetCatalogImportRowsForSuperadmin(batchId, action, ct);

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/catalog/imports/{batchId:guid}/publish")]
    public async Task<IActionResult> PublishCatalogImport(Guid batchId, CancellationToken ct)
    {
        RequireCatalogSuperadmin();
        return Ok(await _publish.PublishAsync(batchId, CurrentGovAdminName(), ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/catalog/imports/{batchId:guid}/discard")]
    public async Task<IActionResult> DiscardCatalogImport(Guid batchId, CancellationToken ct)
    {
        RequireCatalogSuperadmin();
        await _ingestion.DiscardAsync(batchId, ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("management/ai/status")]
    public async Task<IActionResult> AiStatus(CancellationToken ct)
    {
        RequireGovSuperadmin();
        AiBackendHealthWireResponse? health = null;
        AiBackendReadyWireResponse? readiness = null;
        try { health = await _aiIngestion.GetHealthAsync(ct); } catch (AiBackendUnavailableException) { }
        try { readiness = await _aiIngestion.GetReadyAsync(ct); } catch (AiBackendUnavailableException) { }
        var telemetry = await _aiMonitor.GetMetricsAsync(ct);

        var status = health is not null && readiness is not null ? "Healthy" : health is not null || readiness is not null ? "Degraded" : "Unhealthy";
        return Ok(new
        {
            status,
            checkedAt = DateTimeOffset.UtcNow,
            health,
            // readiness's own [JsonPropertyName] attributes are for parsing ai_backend's
            // snake_case wire reply, not for this outbound response -- project to a plain
            // object here so the admin panel gets the camelCase keys its types.ts expects.
            readiness = readiness is null ? null : new
            {
                readiness.Status,
                readiness.ActiveKnowledgeSource,
                readiness.ServiceIndexPrefix,
                readiness.TotalServicesIndexed,
                readiness.EmbeddingDimension
            },
            telemetry,
            telemetryNote = telemetry is null
                ? "GPU host telemetry is unavailable. Deploy the separate read-only ai-monitor sidecar to enable it."
                : "Read-only telemetry collected by the separate GPU-host ai-monitor sidecar."
        });
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/ai/reingest")]
    public async Task<IActionResult> ReingestAiKnowledge([FromBody] ConfirmAiReingestBody body, CancellationToken ct)
    {
        RequireGovSuperadmin();
        if (!body.Confirmed) throw new DomainException("Reingestion requires an explicit confirmation.");
        return Ok(await _aiIngestion.RefreshKnowledgeBaseAsync(ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("management/ai/ask")]
    public async Task<IActionResult> AskGovAi([FromBody] GovAdminAiAskBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Message)) throw new DomainException("A question is required.");
        var sessionCookie = Request.Cookies.TryGetValue(GovAdminSessionMiddleware.CookieName, out var sessionId)
            ? $"{GovAdminSessionMiddleware.CookieName}={sessionId}"
            : string.Empty;
        return Ok(await _aiAssistant.AskAsync(body.Message.Trim(), sessionCookie, ct));
    }

    /// <summary>Department dropdown source for the "create admin" form -- the real, already-live
    /// department catalog (discovery.department_master), not a separate static list.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("reference/departments")]
    public async Task<IActionResult> ReferenceDepartments(CancellationToken ct)
    {
        var departments = await _discovery.ListDepartmentsAsync(ct);
        return Ok(departments.Select(d => new { code = d.Code, name = d.Name }));
    }

    /// <summary>Karnataka district/taluk dropdown source for the "create admin" form -- fixed
    /// reference data (see KarnatakaAdministrativeUnits), not something a client can widen.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("reference/districts")]
    public IActionResult ReferenceDistricts()
    {
        return Ok(KarnatakaAdministrativeUnits.DistrictTaluks.Select(kv => new { district = kv.Key, taluks = kv.Value }));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/overview")]
    public async Task<IActionResult> Overview(CancellationToken ct) => Ok(await _dashboard.GetOverviewAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/departments")]
    public async Task<IActionResult> Departments(CancellationToken ct) => Ok(await _dashboard.GetDepartmentsAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/districts")]
    public async Task<IActionResult> Districts(CancellationToken ct) => Ok(await _dashboard.GetDistrictsAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/discovery")]
    public async Task<IActionResult> Discovery(CancellationToken ct) => Ok(await _dashboard.GetDiscoveryAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/citizens")]
    public async Task<IActionResult> Citizens(CancellationToken ct) => Ok(await _dashboard.GetCitizensAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/lifecycle")]
    public async Task<IActionResult> Lifecycle(CancellationToken ct) => Ok(await _dashboard.GetLifecycleAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/sakala-sla")]
    public async Task<IActionResult> SakalaSla(CancellationToken ct) => Ok(await _dashboard.GetSakalaSlaAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/service-usage")]
    public async Task<IActionResult> ServiceUsage(DateOnly? since, DateOnly? until, string? departmentCode, CancellationToken ct) =>
        Ok(await _dashboard.GetServiceUsageAsync(since, until, departmentCode, ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/application-funnel")]
    public async Task<IActionResult> ApplicationFunnel(CancellationToken ct) => Ok(await _dashboard.GetApplicationFunnelAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/application-dropoff")]
    public async Task<IActionResult> ApplicationDropoff(CancellationToken ct) => Ok(await _dashboard.GetApplicationDropoffAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/application-aging")]
    public async Task<IActionResult> ApplicationAging(CancellationToken ct) => Ok(await _dashboard.GetApplicationAgingAsync(ct));

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("analytics/sla-trend")]
    public async Task<IActionResult> SlaTrend(CancellationToken ct) => Ok(await _dashboard.GetSlaTrendAsync(ct));

    private void AppendSessionCookie(string sessionId, TimeSpan ttl)
    {
        var sameSite = Enum.TryParse<SameSiteMode>(_cookies.SameSite, true, out var mode) ? mode : SameSiteMode.Lax;
        Response.Cookies.Append(GovAdminSessionMiddleware.CookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookies.Secure,
            SameSite = sameSite,
            Path = _cookies.Path,
            Expires = DateTimeOffset.UtcNow.Add(ttl)
        });
    }

    private string CurrentGovAdminName() =>
        User.Identities.FirstOrDefault(i => i.AuthenticationType == "SevaGovAdmin")?.Name ?? "unknown-gov-admin";

    private async Task<IActionResult> GetCatalogImportRowsForSuperadmin(Guid batchId, string? action, CancellationToken ct)
    {
        RequireCatalogSuperadmin();
        return Ok(await _ingestion.ListRowsAsync(batchId, action, ct));
    }

    private void RequireCatalogSuperadmin()
    {
        RequireGovSuperadmin("Only a Superadmin can import services into the catalogue.");
    }

    private void RequireGovSuperadmin(string message = "Only a Superadmin can manage AI operations.")
    {
        if (!string.Equals(User.FindFirst("gov_admin_tier")?.Value, "Superadmin", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException(message);
    }

    public sealed record GovAdminLoginBody(string Username, string Password);
    public sealed record SetGovAdminActiveBody(bool IsActive);
    public sealed record SaveServiceNoticeBody(
        string Title, string Message, string Severity, bool IsActive,
        DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);
    public sealed record CreateGovNotificationBody(string Title, string Body);
    public sealed record SetGovNotificationActiveBody(bool IsActive);
    public sealed record SetCatalogActiveBody(bool IsActive);
    public sealed record StartCatalogImportRequest(string SourceFilename, string SourceFormat, IReadOnlyList<CatalogRow> Rows);
    public sealed record StartGovAdminImportRequest(string SourceFilename, string SourceFormat, IReadOnlyList<GovAdminImportRowRequest> Rows);
    public sealed record ConfirmAiReingestBody(bool Confirmed);
    public sealed record GovAdminAiAskBody(string Message);
}
