using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IGovAdminAuthService
{
    Task<GovAdminLoginResult> LoginAsync(GovAdminLoginRequest request, CancellationToken ct);
    Task LogoutAsync(GovAdminLogoutRequest request, CancellationToken ct);
    Task<GovAdminMeResult> GetMeAsync(CancellationToken ct);
}
