using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;

namespace SevaOne.Domain.Models;

/// <summary>
/// payment_status_post — the department's fee-payment status callback. This row is a
/// compliance/audit record of what was presented at call time, so it snapshots the token's
/// hash/validity rather than only holding a live FK: if the token row is later purged or
/// rotated, this payment audit trail must still show exactly what was valid at POST time.
/// <see cref="IdempotencyKey"/> guards the same webhook-retry risk as the wallet ledger — a
/// department retrying its POST must not double-record.
/// </summary>
public sealed class PaymentStatusPost : BaseModel
{
    public Guid? CitizenId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? SchemeId { get; set; }

    /// <summary>FK for convenience joins — nullable because the row must survive even if the token is later hard-purged (retention policy), per the snapshot fields below.</summary>
    public Guid? SevaOneTokenId { get; set; }

    /// <summary>Full JWS as presented on this call.</summary>
    public string TokenValue { get; set; } = string.Empty;

    /// <summary>Snapshot of the token's hash at POST time — survives token row purge/rotation.</summary>
    public string TokenHashAtPost { get; set; } = string.Empty;

    public PaymentPostStatus PaymentStatus { get; set; }
    public decimal? Amount { get; set; }
    public DateTimeOffset PostedAt { get; set; }

    /// <summary>Midnight-same-day boundary that made this token valid for this call — snapshot, not a live lookup.</summary>
    public DateTimeOffset TokenValidTill { get; set; }

    /// <summary>Caller-supplied dedupe key for the department's webhook-style POST.</summary>
    public string? IdempotencyKey { get; set; }
}
