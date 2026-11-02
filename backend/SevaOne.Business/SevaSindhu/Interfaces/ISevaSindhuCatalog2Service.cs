using SevaOne.Business.SevaSindhu.DTOs;

namespace SevaOne.Business.SevaSindhu.Interfaces;

public interface ISevaSindhuCatalog2Service
{
    Task<Catalog2RedirectResult> GenerateRedirectAsync(GenerateCatalog2RedirectRequest request, CancellationToken ct);
}
