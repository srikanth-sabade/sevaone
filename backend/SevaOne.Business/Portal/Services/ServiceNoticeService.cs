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

public sealed class ServiceNoticeService : IServiceNoticeService
{
    private static readonly HashSet<string> Severities = new(StringComparer.OrdinalIgnoreCase)
    {
        "info", "warning", "critical"
    };

    private readonly IServiceNoticeRepository _repo;
    private readonly ICurrentGovAdmin _current;
    private readonly IClock _clock;
    private readonly IGovAdminActivityLogRepository _activityLog;

    public ServiceNoticeService(IServiceNoticeRepository repo, ICurrentGovAdmin current, IClock clock, IGovAdminActivityLogRepository activityLog)
    {
        _repo = repo;
        _current = current;
        _clock = clock;
        _activityLog = activityLog;
    }

    public async Task<PublicServiceNoticeDto?> GetPublicAsync(CancellationToken ct)
    {
        var notice = await _repo.GetAsync(activeOnly: true, ct);
        return notice is null ? null : new PublicServiceNoticeDto(
            notice.Title, notice.Message, notice.Severity, notice.StartsAt, notice.EndsAt, notice.UpdatedAt);
    }

    public async Task<ServiceNoticeDto?> GetForAdminAsync(CancellationToken ct)
    {
        RequireSuperadmin();
        var notice = await _repo.GetAsync(activeOnly: false, ct);
        return notice is null ? null : ToAdminDto(notice);
    }

    public async Task<IReadOnlyList<ServiceNoticeHistoryDto>> GetHistoryAsync(CancellationToken ct)
    {
        RequireSuperadmin();
        var history = await _repo.ListHistoryAsync(ct);
        return history.Select(static notice => new ServiceNoticeHistoryDto(
            notice.Id, notice.Title, notice.Message, notice.Severity, notice.WasActive,
            notice.StartsAt, notice.EndsAt, notice.SavedAt)).ToArray();
    }

    public async Task<ServiceNoticeDto> SaveAsync(SaveServiceNoticeRequest request, CancellationToken ct)
    {
        RequireSuperadmin();

        var title = request.Title.Trim();
        var message = request.Message.Trim();
        var severity = request.Severity.Trim().ToLowerInvariant();

        if (title.Length is < 3 or > 120)
        {
            throw new DomainException("The notice title must be between 3 and 120 characters.");
        }

        if (message.Length is < 3 or > 500)
        {
            throw new DomainException("The notice message must be between 3 and 500 characters.");
        }

        if (!Severities.Contains(severity))
        {
            throw new DomainException("Choose Info, Warning, or Critical severity.");
        }

        if (request.StartsAt.HasValue && request.EndsAt.HasValue && request.EndsAt <= request.StartsAt)
        {
            throw new DomainException("The end time must be later than the start time.");
        }

        var saved = await _repo.UpsertAsync(new ServiceNotice
        {
            Title = title,
            Message = message,
            Severity = severity,
            IsActive = request.IsActive,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            UpdatedAt = _clock.UtcNow
        }, _current.GovAdminId, ct);

        await LogActivityAsync("notice.save", title, ct);

        return ToAdminDto(saved);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        RequireSuperadmin();
        await _repo.StopAsync(_current.GovAdminId, ct);
        await LogActivityAsync("notice.stop", null, ct);
    }

    private void RequireSuperadmin()
    {
        if (_current.Tier != GovAdminTier.Superadmin)
        {
            throw new ForbiddenException("Only a Superadmin can manage service notices.");
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

    private static ServiceNoticeDto ToAdminDto(ServiceNotice notice) => new(
        notice.Title, notice.Message, notice.Severity, notice.IsActive,
        notice.StartsAt, notice.EndsAt, notice.UpdatedAt);
}
