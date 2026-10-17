using SevaOne.Business.Portal.DTOs;
namespace SevaOne.Business.Portal.Interfaces;

public interface IPortalDeptFacingService
{
    Task<UserDetailsResult> GetUserDetailsAsync(GetUserDetailsRequest request, CancellationToken ct);
    Task<PostPaymentStatusResult> PostPaymentStatusAsync(PostPaymentStatusRequest request, CancellationToken ct);

    /// <summary>Department -> SevaOne: application done, mint a one-time return token for the browser redirect back.</summary>
    Task<CompleteApplicationResult> CompleteApplicationAsync(CompleteApplicationRequest request, CancellationToken ct);
}
