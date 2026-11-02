using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>The status check of the token integration (Seva Sindhu V3, URL 3): we ask the department for an
/// application's status by its GSC number and bring the application up to date.</summary>
public interface ITokenStatusService
{
    /// <summary>The signed-in citizen's own application. Asks the department at most once every few minutes;
    /// in between it returns what it already knows. Null when the application has no status check.</summary>
    Task<TokenStatusRefreshDto?> RefreshForCitizenAsync(string reference, CancellationToken ct);

    /// <summary>Background: looks at the applications that are due and brings them up to date. Returns how many were asked.</summary>
    Task<int> PollDueAsync(int limit, CancellationToken ct);

    /// <summary>Admin: calls the status API saved for a service with a sample GSC number and shows what came back.</summary>
    Task<TokenStatusTestResult> TestAsync(string departmentCode, string serviceCode, string gscNumber, CancellationToken ct);
}
