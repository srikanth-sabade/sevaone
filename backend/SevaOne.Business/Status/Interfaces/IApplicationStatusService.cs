using SevaOne.Business.Status.DTOs;
namespace SevaOne.Business.Status.Interfaces;

public interface IApplicationStatusService
{
    Task<IReadOnlyList<ApplicationSummaryDto>> ListMineAsync(CancellationToken ct);
    Task<ApplicationDetailDto> GetAsync(GetApplicationRequest request, CancellationToken ct);

    /// <summary>Shared with SubmitApplication, which needs the freshly-created application's full detail in its response.</summary>
    Task<ApplicationDetailDto> GetDetailForCitizenAsync(string reference, Guid citizenId, CancellationToken ct);
    Task<TrackResultDto> TrackAsync(TrackApplicationRequest request, CancellationToken ct);

    /// <summary>Department fee/status callback. Returns false when the department re-posts a status the application already has (no-op, not an error).</summary>
    Task<bool> ApplyDepartmentStatusAsync(ApplyDepartmentStatusRequest request, CancellationToken ct);
}
