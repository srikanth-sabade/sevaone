using SevaOne.Business.Orchestration.DTOs;
using SevaOne.Business.Orchestration.Interfaces;
using System.Text.Json;
using SevaOne.Business.Common;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Connectors;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Orchestration.Services;

public sealed class DepartmentSessionService : IDepartmentSessionService
{
    private readonly IPortalRepository _portal;
    private readonly IDiscoveryRepository _discovery;
    private readonly IOrchestrationRepository _orchestration;
    private readonly IAuditRepository _audit;
    private readonly IDepartmentConnectorRegistry _connectors;
    private readonly IPayloadProtector _protector;
    private readonly IClock _clock;
    private readonly ICurrentCitizen _current;

    public DepartmentSessionService(
        IPortalRepository portal,
        IDiscoveryRepository discovery,
        IOrchestrationRepository orchestration,
        IAuditRepository audit,
        IDepartmentConnectorRegistry connectors,
        IPayloadProtector protector,
        IClock clock,
        ICurrentCitizen current)
    {
        _portal = portal;
        _discovery = discovery;
        _orchestration = orchestration;
        _audit = audit;
        _connectors = connectors;
        _protector = protector;
        _clock = clock;
        _current = current;
    }

    public async Task<DepartmentSessionDto> OpenAsync(OpenDepartmentSessionRequest request, CancellationToken ct)
    {
        var citizenId = _current.CitizenId ?? throw new ForbiddenException("Not authenticated.");
        var result = await GetOrOpenAsync(citizenId, request.DepartmentCode, ct);
        return new DepartmentSessionDto(request.DepartmentCode, result.Handle.ExpiresAt, result.Reused);
    }

    public async Task<OpenedDepartmentSession> GetOrOpenAsync(Guid citizenId, string departmentCode, CancellationToken ct)
    {
        var department = await _discovery.GetDepartmentByCodeAsync(departmentCode, ct)
                          ?? throw new NotFoundException($"Department '{departmentCode}' was not found.");

        var existing = await _orchestration.GetActiveDepartmentSessionAsync(citizenId, department.Id, ct);
        if (existing is not null && existing.ExpiresAt > _clock.UtcNow.AddMinutes(1))
        {
            var handle = JsonSerializer.Deserialize<DepartmentSessionHandle>(_protector.Unprotect(existing.EncryptedPayload))
                         ?? throw new InvalidOperationException("Corrupt department session.");
            return new OpenedDepartmentSession(handle, true);
        }

        var citizen = await _portal.GetCitizenByIdAsync(citizenId, ct)
                      ?? throw new NotFoundException("Citizen not found.");
        var connector = _connectors.Get(department.Code);
        var opened = await connector.OpenSessionAsync(
            new CitizenContext(citizen.Id, citizen.Wso2Subject, citizen.Name, citizen.Mobile, citizen.AadhaarMasked, citizen.SevaId),
            ct);

        await _orchestration.InsertDepartmentSessionAsync(new DepartmentSession
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            DepartmentId = department.Id,
            EncryptedPayload = _protector.Protect(JsonSerializer.Serialize(opened)),
            IssuedAt = _clock.UtcNow,
            ExpiresAt = opened.ExpiresAt
        }, ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            Action = "department.session.open",
            Resource = department.Code,
            Detail = $"expires {opened.ExpiresAt:o}",
            At = _clock.UtcNow
        }, ct);

        return new OpenedDepartmentSession(opened, false);
    }
}
