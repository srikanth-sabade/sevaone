using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

public sealed class DeptTokenGate : IDeptTokenGate
{
    private readonly IPortalRepository _portal;
    private readonly IDiscoveryRepository _discovery;
    private readonly IJwsTokenService _jws;
    private readonly IApplyRedirectProtector _redirectProtector;
    private readonly IClock _clock;

    public DeptTokenGate(
        IPortalRepository portal,
        IDiscoveryRepository discovery,
        IJwsTokenService jws,
        IApplyRedirectProtector redirectProtector,
        IClock clock)
    {
        _portal = portal;
        _discovery = discovery;
        _jws = jws;
        _redirectProtector = redirectProtector;
        _clock = clock;
    }

    public async Task<DeptTokenResolution> ResolveAsync(string departmentId, string encryptedTokenValue, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(departmentId) || string.IsNullOrWhiteSpace(encryptedTokenValue))
        {
            return DeptTokenResolution.Fail("missing_department_or_token");
        }

        // The department hands back exactly what PortalApplyService put in the redirect URL
        // -- the encrypted token, not the plaintext JWS. Decrypt first; everything below
        // (hash lookup, signature check) operates on the recovered plaintext, unchanged.
        var tokenValue = _redirectProtector.TryUnprotect(encryptedTokenValue);
        if (tokenValue is null)
        {
            return DeptTokenResolution.Fail("redirect_decrypt_failed");
        }

        var hash = _jws.ComputeHash(tokenValue);
        var token = await _portal.GetTokenByHashAsync(hash, ct);
        if (token is null)
        {
            return DeptTokenResolution.Fail("token_not_found");
        }

        // Deliberately NOT checking token.IsActive/RevokedAt here -- per this interface's own
        // doc comment, ResolveAsync is a neutral lookup step; each caller checks the validity
        // rule it actually cares about. Get User Details enforces single-use itself via the
        // atomic TryRedeemAsync (which correctly reports "already used", SEVA-DEPT-4003, on a
        // replay). Payment Status POST never redeems and has its own independent
        // MidnightValidUntil window -- it must keep working even after Get User Details has
        // already redeemed the same token, exactly as DEPARTMENT_INTEGRATION_GUIDE.md Section
        // 3.2 documents ("no need to call User Details first"). Checking IsActive/RevokedAt here
        // broke that: a token already redeemed by Get User Details would make Payment Status
        // fail with SEVA-DEPT-4001 ("Token is invalid"), even though it was never meant to
        // require an unredeemed token -- confirmed live by testing both call orders.
        var department = await _discovery.GetDepartmentByIdAsync(token.DepartmentId, ct);
        if (department is null || !string.Equals(department.Code, departmentId, StringComparison.Ordinal))
        {
            return DeptTokenResolution.Fail("department_mismatch");
        }

        if (!_jws.VerifySignature(tokenValue))
        {
            return DeptTokenResolution.Fail("signature_invalid");
        }

        return DeptTokenResolution.Ok(token);
    }

    public Task<bool> TryRedeemAsync(SevaOneToken token, CancellationToken ct) =>
        _portal.TryRedeemTokenAsync(token.Id, _clock.UtcNow, ct);
}
