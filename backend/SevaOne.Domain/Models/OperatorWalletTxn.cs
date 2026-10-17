using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;

namespace SevaOne.Domain.Models;

/// <summary>
/// operator_wallet_txn — append-only ledger of Assisted Mode wallet deducts.
/// <see cref="Amount"/> is always the positive magnitude of the movement;
/// <see cref="TxnStatus"/> carries the outcome, never a signed number.
/// <see cref="IdempotencyKey"/> exists because this is money crossing a network boundary
/// (operator UI -> BFF): a retried/duplicated submit must not double-charge the wallet.
/// </summary>
public sealed class OperatorWalletTxn : BaseModel
{
    public Guid OperatorId { get; set; }

    /// <summary>The citizen the operator is helping.</summary>
    public Guid CitizenId { get; set; }

    public Guid? DepartmentId { get; set; }
    public Guid? SchemeId { get; set; }

    /// <summary>Always > 0. What changed hands, not a running balance.</summary>
    public decimal Amount { get; set; }

    public WalletTxnStatus TxnStatus { get; set; }
    public Guid? SevaOneTokenId { get; set; }

    /// <summary>Caller-supplied dedupe key — unique when present, so a retried apply-with-wallet-deduct request can't charge twice.</summary>
    public string? IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
