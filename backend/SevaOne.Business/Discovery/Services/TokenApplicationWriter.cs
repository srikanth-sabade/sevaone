using System.Text.Json;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Notification;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>What is needed to put a token-integration application on the citizen's dashboard.</summary>
public sealed record TokenApplicationNew(
    Guid CitizenId, Guid ServiceId, Guid DepartmentId, string DepartmentCode, string ServiceName, string Reference,
    string? DepartmentReference, ApplicationStatus Status, DateTimeOffset SubmittedAt, int SakalaDays, string Note,
    string AuditAction, string? CallerIp);

/// <summary>Creates the application record (with its first timeline entry, the "submitted" message and an audit entry) for an
/// application handed to an outside system. Used by the Seva Sindhu acknowledgement and by the Kaveri status check, so both
/// show up on the same dashboard the same way.</summary>
public sealed class TokenApplicationWriter
{
    private readonly IOrchestrationRepository _orchestration;
    private readonly IPortalRepository _portal;
    private readonly INotificationRepository _notification;
    private readonly IAuditRepository _audit;
    private readonly IClock _clock;

    public TokenApplicationWriter(
        IOrchestrationRepository orchestration, IPortalRepository portal, INotificationRepository notification, IAuditRepository audit, IClock clock)
    {
        _orchestration = orchestration;
        _portal = portal;
        _notification = notification;
        _audit = audit;
        _clock = clock;
    }

    public async Task CreateAsync(TokenApplicationNew n, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var citizen = await _portal.GetCitizenByIdAsync(n.CitizenId, ct);
        var application = new ServiceApplication
        {
            Id = Guid.NewGuid(),
            SevaReference = n.Reference,
            DepartmentReference = n.DepartmentReference,
            CitizenId = n.CitizenId,
            SchemeId = n.ServiceId,
            DepartmentId = n.DepartmentId,
            Status = n.Status,
            SubmittedAt = n.SubmittedAt,
            SakalaDueAt = n.SubmittedAt.AddDays(Math.Max(n.SakalaDays, 0)),
            ApplicantName = citizen?.Name ?? "",
            Address = citizen?.Address ?? "",
            District = citizen?.District,
            Taluk = citizen?.Taluk,
        };
        await _orchestration.SubmitApplicationAsync(application, new ApplicationEvent
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, Status = n.Status, Note = n.Note, At = now,
        }, uow: null, ct);

        await _orchestration.InsertOutboxMessageAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "application.submitted",
            Payload = JsonSerializer.Serialize(new
            {
                sevaReference = n.Reference, departmentReference = n.DepartmentReference, departmentCode = n.DepartmentCode,
                serviceName = n.ServiceName, citizenId = n.CitizenId,
            }),
            CreatedAt = now,
        }, uow: null, ct);

        if (!string.IsNullOrWhiteSpace(citizen?.Mobile))
        {
            await _notification.InsertAsync(new NotificationMessage
            {
                Id = Guid.NewGuid(), CitizenId = n.CitizenId, Channel = "sms", Recipient = citizen!.Mobile!, Subject = "Application submitted",
                Body = $"Your application {n.Reference} for {n.ServiceName} was submitted to the department.", CreatedAt = now,
            }, ct);
        }

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(), CitizenId = n.CitizenId, Action = n.AuditAction, Resource = n.Reference,
            Detail = $"{n.ServiceName} via {n.DepartmentCode}. {n.Note}", IpAddress = n.CallerIp, At = now,
        }, ct);
    }
}
