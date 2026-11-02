using SevaOne.Business.Common;
using SevaOne.Domain.Enums;

namespace SevaOne.Services.Auth;

public sealed class HttpCurrentGovAdmin : ICurrentGovAdmin
{
    private readonly IHttpContextAccessor _http;

    public HttpCurrentGovAdmin(IHttpContextAccessor http)
    {
        _http = http;
    }

    public bool IsAuthenticated => GovAdminId.HasValue;

    public Guid? GovAdminId
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirst("gov_admin_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Wso2Subject => _http.HttpContext?.User.FindFirst("gov_admin_wso2_subject")?.Value;

    public string? DisplayName => _http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

    public GovAdminTier? Tier
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirst("gov_admin_tier")?.Value;
            return Enum.TryParse<GovAdminTier>(value, out var tier) ? tier : null;
        }
    }

    public string? AssignedDistrict => _http.HttpContext?.User.FindFirst("gov_admin_district")?.Value;

    public string? AssignedTaluk => _http.HttpContext?.User.FindFirst("gov_admin_taluk")?.Value;

    public string? AssignedDepartment => _http.HttpContext?.User.FindFirst("gov_admin_department")?.Value;

    public string? SessionId => _http.HttpContext?.User.FindFirst("gov_admin_session_id")?.Value;

    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
