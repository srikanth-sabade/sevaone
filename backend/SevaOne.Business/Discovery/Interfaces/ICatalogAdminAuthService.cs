using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>Catalog admin login/logout -- separate identity/session from citizen (Seva) and operator (SevaOperator), mirroring IOperatorAuthService.</summary>
public interface ICatalogAdminAuthService
{
    Task<CatalogAdminLoginResult> LoginAsync(CatalogAdminLoginRequest request, CancellationToken ct);
    Task LogoutAsync(CatalogAdminLogoutRequest request, CancellationToken ct);
}
