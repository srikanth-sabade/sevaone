using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IOperatorClaimService
{
    Task<OperatorClaimStartResult> StartAsync(OperatorClaimStartRequest request, CancellationToken ct);
    Task<OperatorClaimVerifyResult> VerifyAsync(OperatorClaimVerifyRequest request, CancellationToken ct);
}
