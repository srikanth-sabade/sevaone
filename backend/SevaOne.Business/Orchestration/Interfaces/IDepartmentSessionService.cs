using SevaOne.Business.Orchestration.DTOs;
namespace SevaOne.Business.Orchestration.Interfaces;

public interface IDepartmentSessionService
{
    /// <summary>Reuses a live session (with >1 minute of headroom) for this citizen+department, or opens a fresh one via the department connector and persists the encrypted handle.</summary>
    Task<OpenedDepartmentSession> GetOrOpenAsync(Guid citizenId, string departmentCode, CancellationToken ct);

    /// <summary>Controller-facing entry point: resolves the current citizen and opens/reuses the session.</summary>
    Task<DepartmentSessionDto> OpenAsync(OpenDepartmentSessionRequest request, CancellationToken ct);
}
