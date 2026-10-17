using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Domain.Reference;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.GovAdmin;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

/// <summary>
/// Superadmin creates gov-admin accounts (username + role + scope only -- the new admin sets
/// their own password via GovAdminClaimService's first-time-login claim, never Superadmin).
/// ChiefMinister can view the same list but has no create/deactivate access -- per the role
/// spec, ChiefMinister sees everything but controls nothing about admin accounts.
/// </summary>
public sealed class GovAdminManagementService : IGovAdminManagementService
{
    private readonly IPortalRepository _portal;
    private readonly IDiscoveryRepository _discovery;
    private readonly ICurrentGovAdmin _current;
    private readonly IClock _clock;
    private readonly IGovAdminSessionStore _sessions;
    private readonly IGovAdminActivityLogRepository _activityLog;

    public GovAdminManagementService(IPortalRepository portal, IDiscoveryRepository discovery, ICurrentGovAdmin current, IClock clock, IGovAdminSessionStore sessions, IGovAdminActivityLogRepository activityLog)
    {
        _portal = portal;
        _discovery = discovery;
        _current = current;
        _clock = clock;
        _sessions = sessions;
        _activityLog = activityLog;
    }

    public async Task<CreateGovAdminResult> CreateAsync(CreateGovAdminRequest request, CancellationToken ct)
    {
        RequireTier(GovAdminTier.Superadmin, "creating an admin account");

        var username = (request.Username ?? "").Trim();
        var displayName = (request.DisplayName ?? "").Trim();
        if (username.Length < 3)
        {
            throw new DomainException("Enter a username of at least 3 characters.");
        }
        if (displayName.Length < 2)
        {
            throw new DomainException("Enter a display name.");
        }
        if (!Enum.TryParse<GovAdminTier>(request.Tier, out var tier))
        {
            throw new DomainException("Select a valid role.");
        }

        var (district, taluk, department) = await ValidateScopeAsync(tier, request.AssignedDistrict, request.AssignedTaluk, request.AssignedDepartment, ct);

        var id = Guid.NewGuid();
        var (success, message, resultId) = await _portal.CreateGovAdminAsync(
            id, username, displayName, tier.ToString(), district, taluk, department, _clock.UtcNow, ct);

        if (!success || resultId is null)
        {
            throw new DomainException(message);
        }

        await LogActivityAsync("admin.create", username, $"tier={tier}", ct);

        return new CreateGovAdminResult(resultId.Value, username);
    }

    public async Task<IReadOnlyList<GovAdminListItem>> ListAsync(CancellationToken ct)
    {
        RequireTier(GovAdminTier.Superadmin, GovAdminTier.ChiefMinister, "viewing admin accounts");

        var rows = await _portal.ListGovAdminsAsync(ct);
        return rows.Select(r => new GovAdminListItem(
            r.Id, r.Username, r.DisplayName, r.Tier, r.AssignedDistrict, r.AssignedTaluk,
            r.AssignedDepartment, r.IsActive, r.Claimed, r.CreatedAt)).ToList();
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        RequireTier(GovAdminTier.Superadmin, "activating or deactivating an admin account");

        var found = await _portal.SetGovAdminActiveAsync(id, isActive, ct);
        if (!found)
        {
            throw new NotFoundException("Admin account not found.");
        }

        await LogActivityAsync(isActive ? "admin.reactivate" : "admin.deactivate", id.ToString(), null, ct);
    }

    public async Task<IReadOnlyList<GovAdminSessionDto>> ListSessionsAsync(Guid adminId, CancellationToken ct)
    {
        if (_current.Tier != GovAdminTier.Superadmin && _current.GovAdminId != adminId)
        {
            throw new ForbiddenException("You can only view your own active sessions.");
        }

        var sessions = await _sessions.ListActiveSessionsAsync(adminId, ct);
        return sessions.Select(session => new GovAdminSessionDto(
            session.SessionId, session.GovAdminId, session.DisplayName, session.Tier,
            session.ExpiresAt, string.Equals(session.SessionId, _current.SessionId, StringComparison.Ordinal))).ToList();
    }

    public async Task ForceLogoutAsync(Guid adminId, string sessionId, CancellationToken ct)
    {
        if (_current.Tier != GovAdminTier.Superadmin && _current.GovAdminId != adminId)
        {
            throw new ForbiddenException("You can only end your own sessions.");
        }

        var session = await _sessions.GetAsync(sessionId, ct);
        if (session is null || session.GovAdminId != adminId)
        {
            throw new NotFoundException("Session not found or already expired.");
        }

        await _sessions.DeleteAsync(sessionId, ct);
    }

    private async Task<(string? District, string? Taluk, string? Department)> ValidateScopeAsync(
        GovAdminTier tier, string? district, string? taluk, string? department, CancellationToken ct)
    {
        switch (tier)
        {
            case GovAdminTier.DistrictHead:
                if (string.IsNullOrWhiteSpace(district) || !KarnatakaAdministrativeUnits.IsValidDistrict(district))
                {
                    throw new DomainException("Select a valid district for a District Head.");
                }
                return (district, null, null);

            case GovAdminTier.TalukHead:
                if (string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(taluk)
                    || !KarnatakaAdministrativeUnits.IsValidTaluk(district, taluk))
                {
                    throw new DomainException("Select a valid district and taluk for a Taluk Head.");
                }
                return (district, taluk, null);

            case GovAdminTier.Department:
                if (string.IsNullOrWhiteSpace(department))
                {
                    throw new DomainException("Select a department.");
                }
                var departments = await _discovery.ListDepartmentsAsync(ct);
                if (!departments.Any(d => string.Equals(d.Code, department, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new DomainException("Select a valid department.");
                }
                return (null, null, department);

            default:
                // Superadmin/ChiefMinister: statewide, no scope accepted even if the caller sent one.
                return (null, null, null);
        }
    }

    private void RequireTier(GovAdminTier allowed, string action)
    {
        if (_current.Tier != allowed)
        {
            throw new ForbiddenException($"Only a Superadmin can perform this action: {action}.");
        }
    }

    private void RequireTier(GovAdminTier allowedA, GovAdminTier allowedB, string action)
    {
        if (_current.Tier != allowedA && _current.Tier != allowedB)
        {
            throw new ForbiddenException($"You don't have access to: {action}.");
        }
    }

    private Task LogActivityAsync(string action, string target, string? detail, CancellationToken ct) =>
        _activityLog.InsertAsync(new GovAdminActivityLog
        {
            Id = Guid.NewGuid(),
            ActorUsername = _current.Wso2Subject ?? "",
            ActorDisplayName = _current.DisplayName ?? "",
            Action = action,
            Target = target,
            Detail = detail,
            At = _clock.UtcNow
        }, ct);
}
