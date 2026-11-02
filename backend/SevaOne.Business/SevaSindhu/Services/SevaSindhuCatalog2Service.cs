using Microsoft.Extensions.Options;
using SevaOne.Business.SevaSindhu.DTOs;
using SevaOne.Business.SevaSindhu.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Business.SevaSindhu.Services;

/// <summary>
/// The generic Sakala Token Generation API + redirect (every department except
/// BESCOM/CESCOM/MESCOM, which have their own separate endpoint -- SevaSindhuEscomService's
/// job). Called by PortalApplyService's operator-assisted apply branch when
/// Scheme.IntegrationMethod is 'sakala_token' (see V60__sevasindhu_service_id_mapping.sql,
/// V61__operator_only_sakala_services.sql) -- Service Catalogue 2.0, which used to be the only
/// caller, has been retired. Class name kept as-is rather than renamed along with the retired
/// page it originally served.
/// </summary>
public sealed class SevaSindhuCatalog2Service : ISevaSindhuCatalog2Service
{
    private readonly ISevaSindhuCatalog2Client _client;
    private readonly SevaSindhuCatalog2Options _options;
    private readonly ICurrentApplicantResolver _applicant;

    public SevaSindhuCatalog2Service(ISevaSindhuCatalog2Client client, IOptions<SevaSindhuCatalog2Options> options, ICurrentApplicantResolver applicant)
    {
        _client = client;
        _options = options.Value;
        _applicant = applicant;
    }

    public async Task<Catalog2RedirectResult> GenerateRedirectAsync(GenerateCatalog2RedirectRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceId))
        {
            throw new DomainException("Service ID is required.");
        }

        var serviceId = request.ServiceId.Trim();
        var userId = await _applicant.ResolveUserIdAsync(_options.UserId, ct);
        var token = await _client.GenerateTokenAsync(serviceId, userId, ct);

        // Same query-string-append convention as PortalApplyService/SevaSindhuEscomService --
        // Uri.EscapeDataString per value. Unlike the ESCOM redirect, this one also repeats
        // ServiceID and adds KioskType, per the supplied integration details.
        var destination = _options.ServiceRedirectUrls.GetValueOrDefault(serviceId, _options.RedirectUrl);
        var separator = destination.Contains('?') ? '&' : '?';
        var redirectUrl = $"{destination}{separator}" +
                           $"User_Id={Uri.EscapeDataString(userId)}" +
                           $"&Token={Uri.EscapeDataString(token)}" +
                           $"&KioskType={Uri.EscapeDataString(_options.KioskType)}" +
                           $"&ServiceID={Uri.EscapeDataString(serviceId)}";

        return new Catalog2RedirectResult(redirectUrl);
    }
}
