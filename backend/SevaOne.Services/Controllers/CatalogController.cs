using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;

namespace SevaOne.Services.Controllers;

[Route("api/v1")]
public sealed class CatalogController : SevaControllerBase
{
    private readonly ICatalogService _catalog;
    private readonly ICatalogHierarchyService _hierarchy;
    private readonly IPortalApplyService _apply;
    private readonly IApplyClickTrackingService _tracking;

    public CatalogController(ICatalogService catalog, ICatalogHierarchyService hierarchy, IPortalApplyService apply, IApplyClickTrackingService tracking)
    {
        _catalog = catalog;
        _hierarchy = hierarchy;
        _apply = apply;
        _tracking = tracking;
    }

    [HttpGet("departments")]
    public async Task<IActionResult> ListDepartments(CancellationToken ct) => Ok(await _catalog.ListDepartmentsAsync(ct));

    [HttpGet("schemes")]
    public async Task<IActionResult> ListSchemes(string? department, string? q, string? category, CancellationToken ct) =>
        Ok(await _catalog.ListSchemesAsync(new ListSchemesRequest(department, q, category), ct));

    /// <summary>The landing page's "Frequently requested this month" section -- ranked by real
    /// apply attempts, not an arbitrary slice of the catalog. Declared as a literal route
    /// ahead of schemes/{code} below; ASP.NET Core's routing already prefers the literal match,
    /// so there's no ambiguity, but keeping it here (not after) documents the intent.</summary>
    [HttpGet("schemes/popular")]
    public async Task<IActionResult> ListPopularSchemes(int? limit, CancellationToken ct) =>
        Ok(await _catalog.ListPopularSchemesAsync(limit ?? 6, ct));

    [HttpGet("search")]
    public async Task<IActionResult> Search(string q, CancellationToken ct)
    {
        var request = new SearchServicesRequest(q);
        await ValidateAsync(request, ct);
        return Ok(await _catalog.SearchServicesAsync(request, ct));
    }

    [HttpGet("schemes/{code}")]
    public async Task<IActionResult> GetScheme(string code, CancellationToken ct) =>
        Ok(await _catalog.GetSchemeAsync(new GetSchemeRequest(code), ct));

    /// <summary>departmentCode is required -- service_id is only unique within a department
    /// (see V39), so the department has to come from the caller, not be inferred.
    ///
    /// Deliberately no [Authorize] -- a plain homepage-fallback service resolves with no login
    /// at all (see CreateApplyTokenAsync). Only a real deep link needs identity, and that case
    /// returns RequiresLogin:true in the response body instead of a 401, since "not logged in
    /// yet" is the expected, majority case here, not an error.</summary>
    [HttpPost("departments/{departmentCode}/schemes/{code}/apply-token")]
    public async Task<IActionResult> CreateApplyToken(string departmentCode, string code, CancellationToken ct) =>
        Ok(await _apply.CreateApplyTokenAsync(new CreateApplyTokenRequest(departmentCode, code), ct));

    public sealed record TrackApplyClickBody(bool AssistedMode = false, long? SevaOneUserId = null);

    /// <summary>Public interest-measurement click count -- deliberately no [Authorize].
    /// ICurrentCitizen is optionally populated by Wso2CitizenAuthMiddleware regardless of this
    /// gate (same pattern as AiChatController). Best-effort: never fails the caller's
    /// navigation, see ApplyClickTrackingService.</summary>
    [EnableRateLimiting("apply-click-track")]
    [HttpPost("departments/{departmentCode}/schemes/{code}/apply-click")]
    public async Task<IActionResult> TrackApplyClick(string departmentCode, string code, [FromBody] TrackApplyClickBody? body, CancellationToken ct) =>
        Ok(await _tracking.TrackAsync(new TrackApplyClickRequest(departmentCode, code, body?.AssistedMode ?? false, body?.SevaOneUserId), ct));

    public sealed record TrackAiApplyClickBody(string ServiceName, string? DepartmentCode = null, string? OperatorHint = null, bool AssistedMode = false, long? SevaOneUserId = null);

    /// <summary>The AI chat's Apply button calls this instead of opening sevaone-rag's own
    /// apply_url directly -- this is now the single source of truth for both the real apply URL
    /// AND the click count, matching the catalogue flow's own apply-click endpoint above except
    /// keyed by service NAME (sevaone-rag has no code that lines up with this catalog's own).
    /// Deliberately no [Authorize], same reasoning as TrackApplyClick above.</summary>
    [EnableRateLimiting("apply-click-track")]
    [HttpPost("schemes/ai-apply-click")]
    public async Task<IActionResult> TrackAiApplyClick([FromBody] TrackAiApplyClickBody body, CancellationToken ct) =>
        Ok(await _tracking.ResolveAndTrackAiApplyAsync(
            new TrackAiApplyClickRequest(body.ServiceName, body.DepartmentCode, body.OperatorHint, body.AssistedMode, body.SevaOneUserId), ct));

    // ---- Service Catalogue (department -> sub-department -> service), role-filtered ----
    // Role is always resolved from the caller's own identity below, never a client-supplied
    // parameter -- a citizen session can't ask for operator-only data by editing a query string.

    [HttpGet("catalog/departments")]
    public async Task<IActionResult> ListCatalogDepartments(CancellationToken ct) =>
        Ok(await _hierarchy.ListDepartmentsAsync(ResolveRole(), ct));

    [HttpGet("catalog/departments/{code}")]
    public async Task<IActionResult> GetCatalogDepartment(string code, CancellationToken ct)
    {
        var department = await _hierarchy.GetDepartmentAsync(code, ResolveRole(), ct);
        return department is null ? NotFound() : Ok(department);
    }

    [HttpGet("catalog/search")]
    public async Task<IActionResult> SearchCatalog(string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new { departments = Array.Empty<object>(), services = Array.Empty<object>() });
        return Ok(await _hierarchy.SearchAsync(q, ResolveRole(), ct));
    }

    [HttpGet("catalog/services/{departmentCode}/{serviceCode}")]
    public async Task<IActionResult> GetCatalogService(string departmentCode, string serviceCode, CancellationToken ct)
    {
        var service = await _hierarchy.GetServiceAsync(departmentCode, serviceCode, ResolveRole(), ct);
        // 404, not a filtered-empty 200 -- a role-excluded service's existence never leaks,
        // same whether it doesn't exist at all or just isn't visible to this caller.
        return service is null ? NotFound() : Ok(service);
    }

    /// <summary>Bulk export for the AI backend's catalog ingestion -- the full citizen-visible
    /// catalog in one call, not paginated (a few hundred/thousand rows, not "big data" -- see
    /// the sizing discussion this replaced). Unauthenticated for now, same as the other public
    /// catalog reads above: locking this down (network boundary, shared credential, or
    /// otherwise) is a deliberately deferred decision, tied to however ai_backend ends up
    /// actually reaching this endpoint in production.</summary>
    [HttpGet("catalog/ai-export")]
    public async Task<IActionResult> GetAiExport(CancellationToken ct) => Ok(await _hierarchy.ExportForAiAsync(ct));

    /// <summary>Operator if the caller has an active SevaOperator identity (present regardless of this endpoint's own auth requirements -- OperatorSessionMiddleware adds it to every request that carries a valid seva.op.sid cookie); citizen otherwise, including anonymous -- matches how the catalogue is publicly browsable today.</summary>
    private string ResolveRole() =>
        User.Identities.Any(i => i.AuthenticationType == "SevaOperator" && i.IsAuthenticated) ? "operator" : "citizen";
}
