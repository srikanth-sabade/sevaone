using Microsoft.Extensions.Options;
using SevaOne.Business.SevaSindhu.DTOs;
using SevaOne.Business.SevaSindhu.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Business.SevaSindhu.Services;

/// <summary>
/// BESCOM/CESCOM/MESCOM's own Sakala token/redirect endpoints (a different API from the
/// generic Sakala flow -- see SevaSindhuCatalog2Service). Called by PortalApplyService's
/// operator-assisted apply branch when Scheme.IntegrationMethod is 'sakala_token_escom' (see
/// V61__operator_only_sakala_services.sql) -- Service Catalogue 2.0, which used to be the only
/// caller, has been retired.
/// </summary>
public sealed class SevaSindhuEscomService : ISevaSindhuEscomService
{
    private readonly ISevaSindhuEscomClient _client;
    private readonly SevaSindhuEscomOptions _options;
    private readonly ICurrentApplicantResolver _applicant;

    public SevaSindhuEscomService(ISevaSindhuEscomClient client, IOptions<SevaSindhuEscomOptions> options, ICurrentApplicantResolver applicant)
    {
        _client = client;
        _options = options.Value;
        _applicant = applicant;
    }

    public async Task<EscomRedirectResult> GenerateRedirectAsync(GenerateEscomRedirectRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceId))
        {
            throw new DomainException("Service ID is required.");
        }

        var userId = await _applicant.ResolveUserIdAsync(_options.UserId, ct);
        var token = await _client.GenerateTokenAsync(request.ServiceId.Trim(), userId, ct);

        // Same query-string-append convention as PortalApplyService's own redirect URL
        // building -- Uri.EscapeDataString per value, no assumption about whether the
        // configured base URL already carries a '?'.
        var separator = _options.RedirectUrl.Contains('?') ? '&' : '?';
        var redirectUrl = $"{_options.RedirectUrl}{separator}" +
                           $"User_Id={Uri.EscapeDataString(userId)}" +
                           $"&Token={Uri.EscapeDataString(token)}";

        return new EscomRedirectResult(redirectUrl);
    }
}
