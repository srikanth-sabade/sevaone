using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

/// <summary>Read-only query surface over portal.gov_admin_activity_log. Writing is done inline
/// by whichever service performs the action (GovAdminAuthService, GovAdminManagementService,
/// ServiceNoticeService, GovNotificationService) via IGovAdminActivityLogRepository directly --
/// this interface exists only for the admin panel's own "who did what" viewer.</summary>
public interface IGovAdminActivityLogService
{
    Task<IReadOnlyList<GovAdminActivityLogDto>> ListAsync(int limit, string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}
