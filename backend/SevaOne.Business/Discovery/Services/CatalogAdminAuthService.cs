using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>Mirrors OperatorAuthService's login flow (timing-safe credential check against a dummy hash, same PBKDF2 verifier) minus its audit-log write -- deliberately left out of this first pass to keep the admin persona's footprint small; add an audit.audit_logs entry here when that's actually wanted.</summary>
public sealed class CatalogAdminAuthService : ICatalogAdminAuthService
{
    private const string DummyHashForTimingParity =
        "210000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly IDiscoveryRepository _discovery;
    private readonly IAdminSessionStore _sessions;

    public CatalogAdminAuthService(IDiscoveryRepository discovery, IAdminSessionStore sessions)
    {
        _discovery = discovery;
        _sessions = sessions;
    }

    public async Task<CatalogAdminLoginResult> LoginAsync(CatalogAdminLoginRequest request, CancellationToken ct)
    {
        var adminCode = (request.AdminCode ?? "").Trim();
        var password = request.Password ?? "";
        if (adminCode.Length < 3 || password.Length < 6)
        {
            throw new ForbiddenException("Invalid admin code or password.");
        }

        var admin = await _discovery.GetCatalogAdminByCodeAsync(adminCode, ct);
        // Verify runs even when admin is null (against a fixed dummy hash) so a nonexistent
        // admin code doesn't return faster than a real one with a wrong password.
        var passwordOk = OperatorPasswordHasher.Verify(password, admin?.PasswordHash ?? DummyHashForTimingParity);
        if (admin is null || !admin.IsActive || !passwordOk)
        {
            throw new ForbiddenException("Invalid admin code or password.");
        }

        var ttl = TimeSpan.FromHours(8);
        var session = new AdminSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            AdminId = admin.Id,
            AdminCode = admin.AdminId,
            AdminName = admin.AdminName,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
        };
        await _sessions.SaveAsync(session, ttl, ct);

        return new CatalogAdminLoginResult(session.SessionId, session.AdminName, ttl);
    }

    public async Task LogoutAsync(CatalogAdminLogoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId)) return;
        await _sessions.DeleteAsync(request.SessionId, ct);
    }
}
