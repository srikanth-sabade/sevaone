using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Infrastructure.Repositories.GovAdmin;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

/// <summary>Mirrors OperatorAuthService, minus the legacy local-password branch: gov admins
/// have exactly one credential path (WSO2 IS), so there's no fallback to consider. Every
/// rejection reason -- unknown WSO2 user, wrong password, valid WSO2 login but not in the
/// SevaOne_GovAdmin group, no matching local gov_admin_master row, or a locally deactivated row
/// -- collapses to the same generic message, same anti-enumeration posture as
/// OperatorAuthService.LoginAsync.</summary>
public sealed class GovAdminAuthService : IGovAdminAuthService
{
    private readonly IPortalRepository _portal;
    private readonly IGovAdminSessionStore _sessions;
    private readonly IWso2GovAdminIdentityClient _wso2;
    private readonly IGovAdminActivityLogRepository _activityLog;
    private readonly ICurrentGovAdmin _current;
    private readonly IClock _clock;

    public GovAdminAuthService(
        IPortalRepository portal,
        IGovAdminSessionStore sessions,
        IWso2GovAdminIdentityClient wso2,
        IGovAdminActivityLogRepository activityLog,
        ICurrentGovAdmin current,
        IClock clock)
    {
        _portal = portal;
        _sessions = sessions;
        _wso2 = wso2;
        _activityLog = activityLog;
        _current = current;
        _clock = clock;
    }

    public async Task<GovAdminLoginResult> LoginAsync(GovAdminLoginRequest request, CancellationToken ct)
    {
        var username = (request.Username ?? "").Trim();
        var password = request.Password ?? "";
        if (username.Length < 3 || password.Length < 6)
        {
            throw new ForbiddenException("Invalid username or password.");
        }

        var creds = await _portal.GetGovAdminByUsernameAsync(username, ct);
        if (creds is null || !creds.IsActive || creds.Wso2Subject is not { } wso2Subject)
        {
            // Unknown username, locally deactivated, or not yet claimed (see
            // V69__gov_admin_master_claim_flow.sql) -- all fail exactly like a bad password, so
            // a login attempt can never reveal which of these is actually true.
            throw new ForbiddenException("Invalid username or password.");
        }

        var loginResult = await _wso2.LoginAsync(wso2Subject, password, ct);
        if (!loginResult.Success || !loginResult.InGovAdminGroup)
        {
            throw new ForbiddenException("Invalid username or password.");
        }

        var now = _clock.UtcNow;
        var ttl = TimeSpan.FromHours(8);
        var session = new GovAdminSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            GovAdminId = creds.Id,
            Wso2Subject = wso2Subject,
            DisplayName = creds.DisplayName,
            Tier = creds.Tier,
            AssignedDistrict = creds.AssignedDistrict,
            AssignedTaluk = creds.AssignedTaluk,
            AssignedDepartment = creds.AssignedDepartment,
            ExpiresAt = now.Add(ttl)
        };
        await _sessions.SaveAsync(session, ttl, ct);

        await _activityLog.InsertAsync(new GovAdminActivityLog
        {
            Id = Guid.NewGuid(),
            ActorUsername = wso2Subject,
            ActorDisplayName = creds.DisplayName,
            Action = "login",
            At = now
        }, ct);

        return new GovAdminLoginResult(session.SessionId, session.DisplayName, session.Tier, ttl);
    }

    public async Task LogoutAsync(GovAdminLogoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return;
        }

        var session = await _sessions.GetAsync(request.SessionId, ct);
        await _sessions.DeleteAsync(request.SessionId, ct);

        if (session is not null)
        {
            await _activityLog.InsertAsync(new GovAdminActivityLog
            {
                Id = Guid.NewGuid(),
                ActorUsername = session.Wso2Subject,
                ActorDisplayName = session.DisplayName,
                Action = "logout",
                At = _clock.UtcNow
            }, ct);
        }
    }

    public Task<GovAdminMeResult> GetMeAsync(CancellationToken ct)
    {
        if (!_current.IsAuthenticated)
        {
            throw new ForbiddenException("Not authenticated as a government admin.");
        }

        return Task.FromResult(new GovAdminMeResult(
            _current.DisplayName ?? "",
            _current.Tier?.ToString() ?? "",
            _current.AssignedDistrict,
            _current.AssignedTaluk,
            _current.AssignedDepartment));
    }
}
