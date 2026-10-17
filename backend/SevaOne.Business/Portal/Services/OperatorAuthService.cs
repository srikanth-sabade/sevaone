using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

public sealed class OperatorAuthService : IOperatorAuthService
{
    private readonly IPortalRepository _portal;
    private readonly IOperatorSessionStore _sessions;
    private readonly IWso2OperatorIdentityClient _wso2;
    private readonly IAuditRepository _audit;
    private readonly ICurrentOperator _current;
    private readonly IClock _clock;

    public OperatorAuthService(
        IPortalRepository portal,
        IOperatorSessionStore sessions,
        IWso2OperatorIdentityClient wso2,
        IAuditRepository audit,
        ICurrentOperator current,
        IClock clock)
    {
        _portal = portal;
        _sessions = sessions;
        _wso2 = wso2;
        _audit = audit;
        _current = current;
        _clock = clock;
    }

    public async Task<OperatorLoginResult> LoginAsync(OperatorLoginRequest request, CancellationToken ct)
    {
        var operatorCode = (request.OperatorCode ?? "").Trim();
        var password = request.Password ?? "";
        if (operatorCode.Length < 3 || password.Length < 6)
        {
            // Same generic failure as a wrong password below -- don't let input-shape alone
            // distinguish "malformed" from "wrong credentials" for an attacker probing codes.
            throw new ForbiddenException("Invalid operator code or password.");
        }

        var creds = await _portal.GetOperatorCredentialsByCodeAsync(operatorCode, ct);

        // Operators claimed via OperatorClaimService authenticate against a real WSO2 IS
        // account (wso2Subject set); pre-seeded legacy operators (R__seed_operator.sql's
        // OPR10001) still have none and fall back to the local PBKDF2 check below -- see
        // OperatorMaster.Wso2Subject's doc comment.
        bool passwordOk;
        if (creds?.Wso2Subject is { } wso2Subject)
        {
            var result = await _wso2.LoginAsync(wso2Subject, password, ct);
            passwordOk = result is { Success: true, InOperatorGroup: true };
        }
        else
        {
            // Verify runs even when creds is null (against a fixed dummy hash) so a nonexistent
            // operator code doesn't return faster than a real one with a wrong password --
            // avoids a timing side-channel for operator-code enumeration.
            passwordOk = OperatorPasswordHasher.Verify(password, creds?.PasswordHash ?? DummyHashForTimingParity);
        }

        if (creds is null || !creds.IsActive || !passwordOk)
        {
            throw new ForbiddenException("Invalid operator code or password.");
        }

        var now = _clock.UtcNow;
        var ttl = TimeSpan.FromHours(8);
        var session = new OperatorSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            OperatorId = creds.Id,
            OperatorCode = creds.OperatorCode,
            OperatorName = creds.OperatorName,
            ExpiresAt = now.Add(ttl)
        };
        await _sessions.SaveAsync(session, ttl, ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = null,
            Action = "operator.login",
            Resource = creds.OperatorCode,
            IpAddress = _current.IpAddress,
            At = now
        }, ct);

        return new OperatorLoginResult(session.SessionId, session.OperatorName, ttl);
    }

    public async Task LogoutAsync(OperatorLogoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return;
        }

        var session = await _sessions.GetAsync(request.SessionId, ct);
        await _sessions.DeleteAsync(request.SessionId, ct);

        if (session is not null)
        {
            await _audit.InsertAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                CitizenId = null,
                Action = "operator.logout",
                Resource = session.OperatorCode,
                IpAddress = _current.IpAddress,
                At = _clock.UtcNow
            }, ct);
        }
    }

    public async Task<OperatorMeResult> GetMeAsync(CancellationToken ct)
    {
        if (!_current.IsAuthenticated)
        {
            throw new ForbiddenException("Not authenticated as an operator.");
        }

        BoundCitizenDto? bound = null;
        if (_current.ActiveCitizenId is { } citizenId)
        {
            var citizen = await _portal.GetCitizenByIdAsync(citizenId, ct);
            if (citizen is not null)
            {
                bound = new BoundCitizenDto(citizen.SevaOneUserId, citizen.Name, citizen.Mobile);
            }
        }

        return new OperatorMeResult(_current.OperatorCode ?? "", _current.OperatorName, bound);
    }

    // A real PBKDF2 hash of an unguessable, never-used password -- fed to Verify() only to
    // burn the same CPU time as a real check when the operator code doesn't exist.
    private const string DummyHashForTimingParity =
        "210000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
}
