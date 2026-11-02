using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Admin console: set up the token integration (department URL, status API, allowed IPs) per service.
/// Superadmin only, same gate as catalogue management. This is separate from the operator-only
/// Sakala setup, which is shown read-only here and never changed by these endpoints.
/// </summary>
[Route("api/v1/admin/gov/management/integrations")]
public sealed class TokenIntegrationAdminController : SevaControllerBase
{
    private readonly ITokenIntegrationAdminService _integrations;
    private readonly ITokenStatusService _status;

    public TokenIntegrationAdminController(ITokenIntegrationAdminService integrations, ITokenStatusService status)
    {
        _integrations = integrations;
        _status = status;
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("types")]
    public async Task<IActionResult> ListTypes(CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _integrations.ListTypesAsync(ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("services")]
    public async Task<IActionResult> ListServices(
        string? search, string? departmentCode, bool configuredOnly = false, bool citizenVisibleOnly = true, int limit = 50, CancellationToken ct = default)
    {
        RequireSuperadmin();
        return Ok(await _integrations.ListServicesAsync(search, departmentCode, configuredOnly, citizenVisibleOnly, limit, ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("services/{departmentCode}/{serviceCode}")]
    public async Task<IActionResult> Get(string departmentCode, string serviceCode, CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _integrations.GetAsync(departmentCode, serviceCode, ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPut("services/{departmentCode}/{serviceCode}")]
    public async Task<IActionResult> Save(string departmentCode, string serviceCode, [FromBody] TokenIntegrationWriteRequest body, CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _integrations.SaveAsync(departmentCode, serviceCode, body, CurrentGovAdminName(), ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpDelete("services/{departmentCode}/{serviceCode}")]
    public async Task<IActionResult> Remove(string departmentCode, string serviceCode, CancellationToken ct)
    {
        RequireSuperadmin();
        await _integrations.RemoveAsync(departmentCode, serviceCode, CurrentGovAdminName(), ct);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("providers/{typeKey}")]
    public async Task<IActionResult> GetProvider(string typeKey, CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _integrations.GetProviderAsync(typeKey, ct));
    }

    /// <summary>Saves the settings shared by every service of a type (for Kaveri: its addresses and the platform's technical
    /// login). The password is write-only: it can be replaced, never read back.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPut("providers/{typeKey}")]
    public async Task<IActionResult> SaveProvider(string typeKey, [FromBody] TokenProviderWriteRequest body, CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _integrations.SaveProviderAsync(typeKey, body, CurrentGovAdminName(), ct));
    }

    /// <summary>Signs in with the saved settings for real and says whether it worked. Never shows the token.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("providers/{typeKey}/test-login")]
    public async Task<IActionResult> TestProviderLogin(string typeKey, CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _integrations.TestProviderLoginAsync(typeKey, ct));
    }

    public sealed record StatusTestBody(string GscNumber);

    /// <summary>Calls the status API saved for this service with a sample GSC number and shows what came back,
    /// so the settings for reading the reply can be checked before real applications depend on them.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("services/{departmentCode}/{serviceCode}/status-test")]
    public async Task<IActionResult> TestStatus(string departmentCode, string serviceCode, [FromBody] StatusTestBody body, CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _status.TestAsync(departmentCode, serviceCode, body.GscNumber, ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("departments/{departmentCode}/credentials")]
    public async Task<IActionResult> ListCredentials(string departmentCode, CancellationToken ct)
    {
        RequireSuperadmin();
        return Ok(await _integrations.ListCredentialsAsync(departmentCode, ct));
    }

    /// <summary>Creates a login for the department's calls to our acknowledgement API. The password is in
    /// this reply only; it is not stored in a form that can be read back.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("departments/{departmentCode}/credentials")]
    public async Task<IActionResult> CreateCredential(string departmentCode, CancellationToken ct)
    {
        RequireSuperadmin();
        Response.Headers.CacheControl = "no-store";
        return Ok(await _integrations.CreateCredentialAsync(departmentCode, CurrentGovAdminName(), ct));
    }

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPost("departments/{departmentCode}/credentials/{id:guid}/reset")]
    public async Task<IActionResult> ResetCredential(string departmentCode, Guid id, CancellationToken ct)
    {
        RequireSuperadmin();
        Response.Headers.CacheControl = "no-store";
        return Ok(await _integrations.ResetCredentialAsync(departmentCode, id, ct));
    }

    public sealed record SetActiveBody(bool IsActive);

    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpPut("departments/{departmentCode}/credentials/{id:guid}/active")]
    public async Task<IActionResult> SetCredentialActive(string departmentCode, Guid id, [FromBody] SetActiveBody body, CancellationToken ct)
    {
        RequireSuperadmin();
        await _integrations.SetCredentialActiveAsync(departmentCode, id, body.IsActive, ct);
        return NoContent();
    }

    private void RequireSuperadmin()
    {
        if (!string.Equals(User.FindFirst("gov_admin_tier")?.Value, "Superadmin", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Only a Superadmin can manage token integrations.");
    }

    private string CurrentGovAdminName() =>
        User.Identities.FirstOrDefault(i => i.AuthenticationType == "SevaGovAdmin")?.Name ?? "unknown-gov-admin";
}
