using SevaOne.Business.Orchestration.DTOs;
using SevaOne.Business.Orchestration.Interfaces;
using System.Text.Json;
using SevaOne.Business.Common;
using SevaOne.Business.Status.DTOs;
using SevaOne.Business.Status.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Connectors;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Notification;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;
using ConnectorSubmitApplicationCommand = SevaOne.Infrastructure.Connectors.SubmitApplicationCommand;

namespace SevaOne.Business.Orchestration.Services;

public sealed class SubmitApplicationService : ISubmitApplicationService
{
    private readonly IDiscoveryRepository _discovery;
    private readonly IOrchestrationRepository _orchestration;
    private readonly IPortalRepository _portal;
    private readonly IAuditRepository _audit;
    private readonly INotificationRepository _notification;
    private readonly IDepartmentSessionService _sessions;
    private readonly IDepartmentConnectorRegistry _connectors;
    private readonly IApplicationStatusService _statusService;
    private readonly ICurrentCitizen _current;
    private readonly IClock _clock;

    public SubmitApplicationService(
        IDiscoveryRepository discovery,
        IOrchestrationRepository orchestration,
        IPortalRepository portal,
        IAuditRepository audit,
        INotificationRepository notification,
        IDepartmentSessionService sessions,
        IDepartmentConnectorRegistry connectors,
        IApplicationStatusService statusService,
        ICurrentCitizen current,
        IClock clock)
    {
        _discovery = discovery;
        _orchestration = orchestration;
        _portal = portal;
        _audit = audit;
        _notification = notification;
        _sessions = sessions;
        _connectors = connectors;
        _statusService = statusService;
        _current = current;
        _clock = clock;
    }

    public async Task<ApplicationDetailDto> SubmitAsync(SubmitApplicationRequest request, CancellationToken ct)
    {
        var citizenId = _current.CitizenId ?? throw new ForbiddenException("Not authenticated.");
        var scheme = await _discovery.GetSchemeByDepartmentAndServiceCodeAsync(request.DepartmentCode, request.ServiceCode, ct)
                     ?? throw new NotFoundException($"Scheme {request.ServiceCode} was not found in department {request.DepartmentCode}.");
        var department = await _discovery.GetDepartmentByIdAsync(scheme.DepartmentId, ct)
                          ?? throw new NotFoundException("Department not found.");

        var opened = await _sessions.GetOrOpenAsync(citizenId, department.Code, ct);

        var connector = _connectors.Get(department.Code);
        _ = await connector.FetchDocumentAsync(opened.Handle, "Aadhaar", ct);
        _ = await connector.InitiatePaymentAsync(opened.Handle, new PaymentRequest(scheme.ServiceCode, 15m, scheme.Name), ct);
        _ = await connector.RequestESignAsync(opened.Handle, new ESignRequest(scheme.ServiceCode, request.ApplicantName), ct);

        var submitted = await connector.SubmitAsync(
            opened.Handle,
            new ConnectorSubmitApplicationCommand(request.ServiceCode, request.ApplicantName, request.Address, request.Purpose, request.District, request.Taluk),
            ct);

        var now = _clock.UtcNow;
        var year = now.Year;
        var sevaRef = $"SVK{year}{Random.Shared.Next(100000, 999999)}";
        while (await _orchestration.GetApplicationByReferenceAsync(sevaRef, ct) is not null)
        {
            sevaRef = $"SVK{year}{Random.Shared.Next(100000, 999999)}";
        }

        var status = ApplicationStatusMapper.FromDepartment(submitted.Status);
        var application = new ServiceApplication
        {
            Id = Guid.NewGuid(),
            SevaReference = sevaRef,
            DepartmentReference = submitted.DepartmentReference,
            CitizenId = citizenId,
            SchemeId = scheme.Id,
            DepartmentId = scheme.DepartmentId,
            Status = status,
            SubmittedAt = now,
            SakalaDueAt = now.AddDays(scheme.SakalaDays),
            ApplicantName = request.ApplicantName,
            Address = request.Address,
            Purpose = request.Purpose,
            District = request.District,
            Taluk = request.Taluk
        };
        var firstEvent = new ApplicationEvent
        {
            Id = Guid.NewGuid(),
            Status = status,
            Note = submitted.Note,
            At = now
        };
        await _orchestration.SubmitApplicationAsync(application, firstEvent, uow: null, ct);

        await _orchestration.InsertOutboxMessageAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "application.submitted",
            Payload = JsonSerializer.Serialize(new
            {
                sevaReference = sevaRef,
                departmentReference = submitted.DepartmentReference,
                departmentCode = department.Code,
                serviceCode = scheme.ServiceCode,
                citizenId
            }),
            CreatedAt = now
        }, uow: null, ct);

        var citizen = await _portal.GetCitizenByIdAsync(citizenId, ct) ?? throw new NotFoundException("Citizen not found.");
        await _notification.InsertAsync(new NotificationMessage
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            Channel = "sms",
            Recipient = citizen.Mobile ?? "",
            Subject = "Application submitted",
            Body = $"Your application {sevaRef} for {scheme.Name} was submitted. Sakala: {scheme.SakalaDays} days.",
            CreatedAt = now
        }, ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            Action = "application.submit",
            Resource = sevaRef,
            Detail = $"{scheme.ServiceCode} via {department.Code}",
            IpAddress = _current.IpAddress,
            At = now
        }, ct);

        return await _statusService.GetDetailForCitizenAsync(sevaRef, citizenId, ct);
    }
}
