using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IGovAdminClaimService
{
    Task<GovAdminClaimStartResult> StartAsync(GovAdminClaimStartRequest request, CancellationToken ct);
    Task<GovAdminClaimVerifyResult> VerifyAsync(GovAdminClaimVerifyRequest request, CancellationToken ct);
}
