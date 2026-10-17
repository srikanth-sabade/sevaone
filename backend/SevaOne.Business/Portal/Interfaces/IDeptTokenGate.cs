using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Models;

namespace SevaOne.Business.Portal.Interfaces;

/// <summary>
/// Shared token-resolution step for every department-facing API. Looks the token up by hash,
/// re-verifies its signature (defense in depth against a tampered DB row), and confirms it
/// was actually issued to the calling department. Does NOT check expiry — that rule differs
/// per caller (30-minute Token Validate/Get User Details vs midnight-same-day Payment Status
/// POST), so each handler checks the field it cares about on the returned row.
/// </summary>
public interface IDeptTokenGate
{
    Task<DeptTokenResolution> ResolveAsync(string departmentId, string tokenValue, CancellationToken ct);

    /// <summary>
    /// One-time redemption for Token Validate / Get User Details — each establishes the
    /// department's own 30-minute session with a single call, so the same JWS must not work
    /// twice. NOT used by Payment Status POST, which has its own independent midnight-validity
    /// rule and is not a one-time call. Returns false on replay via an atomic
    /// UPDATE ... WHERE revoked_at IS NULL (see SevaOneToken doc comment) — no EF change
    /// tracker to fall back on, so this is the only place that race is closed.
    /// </summary>
    Task<bool> TryRedeemAsync(SevaOneToken token, CancellationToken ct);
}

public sealed record DeptTokenResolution(bool Success, SevaOneToken? Token, string? FailureReason)
{
    public static DeptTokenResolution Fail(string reason) => new(false, null, reason);
    public static DeptTokenResolution Ok(SevaOneToken token) => new(true, token, null);
}
