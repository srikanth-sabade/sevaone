using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IOperatorCitizenBindService
{
    Task<SendOperatorCitizenOtpResult> SendOtpAsync(SendOperatorCitizenOtpRequest request, CancellationToken ct);

    Task<BoundCitizenDto> VerifyAndBindAsync(VerifyOperatorCitizenOtpRequest request, CancellationToken ct);

    Task<BoundCitizenDto?> GetBoundCitizenAsync(CancellationToken ct);

    Task UnbindAsync(CancellationToken ct);
}
