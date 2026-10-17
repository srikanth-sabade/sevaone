using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.GovAdmin;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

public sealed class GovNotificationService : IGovNotificationService
{
    private readonly IGovNotificationRepository _repo;
    private readonly ICurrentGovAdmin _current;
    private readonly IGovAdminActivityLogRepository _activityLog;
    private readonly IClock _clock;

    public GovNotificationService(IGovNotificationRepository repo, ICurrentGovAdmin current, IGovAdminActivityLogRepository activityLog, IClock clock)
    {
        _repo = repo;
        _current = current;
        _activityLog = activityLog;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PublicGovNotificationDto>> GetPublicAsync(CancellationToken ct)
    {
        var rows = await _repo.ListAsync(activeOnly: true, ct);
        return rows.Select(static notification => new PublicGovNotificationDto(
            notification.Id, notification.Title, notification.Body, notification.CreatedAt)).ToArray();
    }

    public async Task<IReadOnlyList<GovNotificationDto>> ListForAdminAsync(CancellationToken ct)
    {
        RequireSuperadmin();
        var rows = await _repo.ListAsync(activeOnly: false, ct);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<GovNotificationDto> CreateAsync(CreateGovNotificationRequest request, CancellationToken ct)
    {
        RequireSuperadmin();
        var title = request.Title.Trim();
        var body = request.Body.Trim();
        if (title.Length is < 3 or > 160)
        {
            throw new DomainException("The notification heading must be between 3 and 160 characters.");
        }

        if (body.Length is < 3 or > 5000)
        {
            throw new DomainException("The notification text must be between 3 and 5,000 characters.");
        }

        var saved = await _repo.CreateAsync(new GovNotification
        {
            Title = title,
            Body = body,
            IsActive = true,
            CreatedBy = _current.GovAdminId,
        }, ct);
        await LogActivityAsync("notification.create", title, ct);
        return ToDto(saved);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        RequireSuperadmin();
        if (!isActive)
        {
            await _repo.SetActiveAsync(id, false, _current.GovAdminId, ct);
            await LogActivityAsync("notification.deactivate", id.ToString(), ct);
            return;
        }

        throw new DomainException("Government notifications can only be deactivated after publication.");
    }

    private void RequireSuperadmin()
    {
        if (_current.Tier != GovAdminTier.Superadmin)
        {
            throw new ForbiddenException("Only a Superadmin can manage government notifications.");
        }
    }

    private Task LogActivityAsync(string action, string? target, CancellationToken ct) =>
        _activityLog.InsertAsync(new GovAdminActivityLog
        {
            Id = Guid.NewGuid(),
            ActorUsername = _current.Wso2Subject ?? "",
            ActorDisplayName = _current.DisplayName ?? "",
            Action = action,
            Target = target,
            At = _clock.UtcNow
        }, ct);

    private static GovNotificationDto ToDto(GovNotification notification) => new(
        notification.Id, notification.Title, notification.Body, notification.IsActive,
        notification.CreatedAt, notification.UpdatedAt);
}
