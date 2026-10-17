using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IOperatorAuthService
{
    Task<OperatorLoginResult> LoginAsync(OperatorLoginRequest request, CancellationToken ct);
    Task LogoutAsync(OperatorLogoutRequest request, CancellationToken ct);
    Task<OperatorMeResult> GetMeAsync(CancellationToken ct);
}
