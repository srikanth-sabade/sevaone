using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevaOne.Business.SevaSindhu.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Business.SevaSindhu.Services;

public sealed class AjskRedirectService : IAjskRedirectService
{
    private const string SafeFailureMessage = "Unable to open this service right now. Please try again.";

    private readonly AjskRedirectOptions _options;
    private readonly ILogger<AjskRedirectService> _log;

    public AjskRedirectService(IOptions<AjskRedirectOptions> options, ILogger<AjskRedirectService> log)
    {
        _options = options.Value;
        _log = log;
    }

    public string BuildRedirectUrl(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException(SafeFailureMessage);
        }
        if (string.IsNullOrWhiteSpace(_options.RaisedLoc))
        {
            _log.LogError("AjskRedirect:RaisedLoc is not configured -- refusing to build the AJSK redirect URL.");
            throw new DomainException(SafeFailureMessage);
        }

        var separator = _options.RedirectUrl.Contains('?') ? '&' : '?';
        return $"{_options.RedirectUrl}{separator}" +
               $"UserId={Uri.EscapeDataString(userId)}" +
               $"&RaisedLoc={Uri.EscapeDataString(_options.RaisedLoc)}";
    }
}
