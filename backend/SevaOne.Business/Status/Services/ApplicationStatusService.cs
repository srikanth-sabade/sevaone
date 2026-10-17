using SevaOne.Business.Status.DTOs;
using SevaOne.Business.Status.Interfaces;
using System.Text.Json;
using AutoMapper;
using SevaOne.Business.Common;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Connectors;
using SevaOne.Infrastructure.Repositories.Notification;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Status.Services;

public sealed class ApplicationStatusService : IApplicationStatusService
{
    private readonly IOrchestrationRepository _orchestration;
    private readonly IStatusRepository _status;
    private readonly INotificationRepository _notification;
    private readonly ISakalaClient _sakala;
    private readonly ICurrentCitizen _current;
    private readonly IClock _clock;
    private readonly IMapper _mapper;

    public ApplicationStatusService(
        IOrchestrationRepository orchestration,
        IStatusRepository status,
        INotificationRepository notification,
        ISakalaClient sakala,
        ICurrentCitizen current,
        IClock clock,
        IMapper mapper)
    {
        _orchestration = orchestration;
        _status = status;
        _notification = notification;
        _sakala = sakala;
        _current = current;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ApplicationSummaryDto>> ListMineAsync(CancellationToken ct)
    {
        var citizenId = _current.CitizenId ?? throw new ForbiddenException("Not authenticated.");
        var rows = await _orchestration.ListApplicationsWithNamesForCitizenAsync(citizenId, ct);
        return _mapper.Map<List<ApplicationSummaryDto>>(rows);
    }

    public async Task<ApplicationDetailDto> GetAsync(GetApplicationRequest request, CancellationToken ct)
    {
        var citizenId = _current.CitizenId ?? throw new ForbiddenException("Not authenticated.");
        return await GetDetailForCitizenAsync(request.Reference, citizenId, ct);
    }

    /// <summary>
    /// Tries SevaOne's own application records first (by sevaReference); if the entered value
    /// doesn't match one, falls back to treating it as a real Sakala GSC number and looking it
    /// up live against sakala.kar.nic.in (ISakalaClient). This fallback exists because SevaOne
    /// doesn't yet capture a real GSC number into orchestration.applications.department_reference
    /// for any live application (confirmed: the one application in the dev DB has a hand-typed
    /// test placeholder, not a real one) -- once that capture exists, a SevaOne-owned
    /// application's own department_reference could be looked up the same way without the
    /// citizen re-typing anything, but the citizen-entered fallback here remains correct
    /// regardless (their own Sakala paperwork works even for applications made outside SevaOne).
    /// </summary>
    public async Task<TrackResultDto> TrackAsync(TrackApplicationRequest request, CancellationToken ct)
    {
        var a = await _orchestration.GetApplicationWithNamesByReferenceAsync(request.Reference, ct);
        if (a is not null)
        {
            return new TrackResultDto("sevaone", _mapper.Map<TrackDto>(a), null);
        }

        var sakala = await _sakala.GetAckDataAsync(request.Reference, ct);
        if (sakala is not null)
        {
            return new TrackResultDto("sakala", null, MapSakala(sakala));
        }

        throw new NotFoundException("Application not found.");
    }

    private static SakalaTrackDto MapSakala(SakalaAckRecord r) => new(
        r.GscNo, r.ApplicantName, r.ServiceName, r.InstitutionName, r.OfficerName,
        r.SakalaDate, r.SakalaClosedDate, r.DueDate, r.DaysPending, r.StatusCode, r.StatusDescription,
        MapComplaint(r.FirstComplaint), MapComplaint(r.SecondComplaint));

    private static SakalaComplaintDto? MapComplaint(SakalaComplaint? c) => c is null
        ? null
        : new SakalaComplaintDto(c.ComplaintNo ?? "", c.ComplaintDate, c.ClosedDate, c.DueDate, c.StatusCode, c.StatusDescription, c.DaysPending);

    public async Task<bool> ApplyDepartmentStatusAsync(ApplyDepartmentStatusRequest request, CancellationToken ct)
    {
        var application = await _orchestration.GetApplicationByDepartmentReferenceAsync(request.DepartmentCode, request.DepartmentReference, ct)
                           ?? throw new NotFoundException("Application not found for department reference.", SevaErrorCodes.Dept.ApplicationNotFoundForReference);

        var next = ApplicationStatusMapper.FromDepartment(request.Status);
        if (application.Status == next)
        {
            return false;
        }

        var now = _clock.UtcNow;
        await _orchestration.UpdateApplicationStatusAsync(application.Id, next, ct);
        await _status.InsertApplicationEventAsync(new ApplicationEvent
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Status = next,
            Note = request.Note,
            At = now
        }, ct);

        await _orchestration.InsertOutboxMessageAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "application.status_changed",
            Payload = JsonSerializer.Serialize(new
            {
                sevaReference = application.SevaReference,
                departmentReference = application.DepartmentReference,
                status = ApplicationStatusMapper.ToApi(next),
                note = request.Note
            }),
            CreatedAt = now
        }, uow: null, ct);

        await _notification.InsertAsync(new NotificationMessage
        {
            Id = Guid.NewGuid(),
            CitizenId = application.CitizenId,
            Channel = "sms",
            Recipient = "",
            Subject = "Application status update",
            Body = $"Application {application.SevaReference} is now {ApplicationStatusMapper.ToApi(next)}.",
            CreatedAt = now
        }, ct);

        if (next is ApplicationStatus.Approved or ApplicationStatus.Rejected)
        {
            await _orchestration.InsertOutboxMessageAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Topic = "notification.requested",
                Payload = JsonSerializer.Serialize(new
                {
                    sevaReference = application.SevaReference,
                    channel = "sms",
                    template = next == ApplicationStatus.Approved ? "approved" : "rejected"
                }),
                CreatedAt = now
            }, uow: null, ct);
        }

        return true;
    }

    public async Task<ApplicationDetailDto> GetDetailForCitizenAsync(string reference, Guid citizenId, CancellationToken ct)
    {
        var a = await _orchestration.GetApplicationWithNamesForCitizenAsync(citizenId, reference, ct)
                 ?? throw new NotFoundException("Application not found.");
        var timeline = await _status.GetTimelineAsync(a.Id, ct);

        // Two sources (application row + separately-fetched timeline), so the outer record is
        // hand-built; the timeline list itself reuses the ApplicationEvent -> TimelineDto map.
        return new ApplicationDetailDto(
            a.SevaReference,
            a.DepartmentReference,
            a.ServiceCode,
            a.ServiceName,
            a.DepartmentCode,
            a.DepartmentName,
            ApplicationStatusMapper.ToApi(a.Status),
            a.SubmittedAt,
            a.SakalaDueAt,
            a.ApplicantName,
            a.Address,
            a.Purpose,
            a.District,
            a.Taluk,
            _mapper.Map<List<TimelineDto>>(timeline.OrderBy(t => t.At)));
    }
}
