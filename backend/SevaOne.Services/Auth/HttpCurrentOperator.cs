using SevaOne.Business.Common;
using SevaOne.Infrastructure.Caching;

namespace SevaOne.Services.Auth;

public sealed class HttpCurrentOperator : ICurrentOperator
{
    private readonly IHttpContextAccessor _http;

    public HttpCurrentOperator(IHttpContextAccessor http)
    {
        _http = http;
    }

    public bool IsAuthenticated => OperatorId.HasValue;

    public Guid? OperatorId
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirst("operator_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? OperatorCode => _http.HttpContext?.User.FindFirst("operator_code")?.Value;

    public string? OperatorName =>
        (_http.HttpContext?.Items["OperatorSession"] as OperatorSession)?.OperatorName;

    public string? SessionId => _http.HttpContext?.User.FindFirst("op_session_id")?.Value;

    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public Guid? ActiveCitizenId =>
        (_http.HttpContext?.Items["OperatorSession"] as OperatorSession)?.ActiveCitizenId;
}
