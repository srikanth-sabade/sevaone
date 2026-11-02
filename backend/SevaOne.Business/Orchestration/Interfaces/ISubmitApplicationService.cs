using SevaOne.Business.Orchestration.DTOs;
using SevaOne.Business.Status.DTOs;

namespace SevaOne.Business.Orchestration.Interfaces;

public interface ISubmitApplicationService
{
    Task<ApplicationDetailDto> SubmitAsync(SubmitApplicationRequest request, CancellationToken ct);
}
