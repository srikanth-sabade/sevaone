using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// sevaone_token — the JWS handed to a department in the Apply URL and re-validated on every
/// subsequent department-to-SevaOne call. This table, not Redis, is the source of truth for
/// department-facing token validation — Redis only ever holds the citizen's own portal cookie
/// session, a different, shorter-lived concept.
///
/// Dual expiry:
///  - <see cref="ExpireAt"/> — the normal 30-minute "Token Validate" session rule.
///  - <see cref="MidnightValidUntil"/> — a separate, longer rule that applies ONLY to the
///    Payment Status POST endpoint: valid until midnight of the same calendar day the token
///    was issued, regardless of the 30-minute rule.
/// A token is never physically deleted — see <see cref="RevokedAt"/>/<see cref="IsActive"/>.
/// Redeem/revoke MUST use an atomic UPDATE...WHERE revoked_at IS NULL + rows-affected check,
/// not a read-then-write, to make replay/race detection safe without EF change tracking.
/// </summary>
public sealed class SevaOneToken : BaseModel
{
    public Guid CitizenId { get; set; }

    /// <summary>Denormalized copy of Citizen.SevaOneUserId — what's actually embedded in the JWS claims and the Apply URL, kept here so token lookups/audits don't need a join.</summary>
    public long SevaOneUserId { get; set; }

    /// <summary>Set only when AssistedMode = true.</summary>
    public Guid? OperatorId { get; set; }
    public bool AssistedMode { get; set; }

    public Guid DepartmentId { get; set; }
    public Guid SchemeId { get; set; }

    /// <summary>The signed JWS itself.</summary>
    public string TokenValue { get; set; } = string.Empty;

    /// <summary>SHA-256 hex fingerprint of <see cref="TokenValue"/>. Validate/revoke lookups go through this fixed-width, uniquely-indexed hash instead of scanning the raw JWS.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>30-minute session-validate boundary.</summary>
    public DateTimeOffset ExpireAt { get; set; }

    /// <summary>Midnight-same-day boundary, Payment Status POST only.</summary>
    public DateTimeOffset? MidnightValidUntil { get; set; }

    /// <summary>When/why this token stopped being valid before its natural expiry (logout, security revoke). Null while live.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Where to send the citizen's browser once the department redirects them back to SevaOne. Not every token has one (e.g. Payment Status POST doesn't need it).</summary>
    public string? ReturnUrl { get; set; }
}
