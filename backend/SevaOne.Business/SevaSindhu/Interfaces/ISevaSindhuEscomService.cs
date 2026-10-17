using SevaOne.Business.SevaSindhu.DTOs;

namespace SevaOne.Business.SevaSindhu.Interfaces;

public interface ISevaSindhuEscomService
{
    Task<EscomRedirectResult> GenerateRedirectAsync(GenerateEscomRedirectRequest request, CancellationToken ct);
}
