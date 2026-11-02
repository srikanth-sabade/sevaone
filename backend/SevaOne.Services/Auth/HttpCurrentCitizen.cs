using SevaOne.Business.Common;

namespace SevaOne.Services.Auth;

public sealed class HttpCurrentCitizen : ICurrentCitizen
{
    private readonly IHttpContextAccessor _http;

    public HttpCurrentCitizen(IHttpContextAccessor http)
    {
        _http = http;
    }

    public bool IsAuthenticated => CitizenId.HasValue;

    public Guid? CitizenId
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirst("citizen_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? SessionId => _http.HttpContext?.User.FindFirst("session_id")?.Value;

    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
