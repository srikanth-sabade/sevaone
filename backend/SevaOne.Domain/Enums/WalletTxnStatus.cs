namespace SevaOne.Domain.Enums;

/// <summary>Pending covers the async gap between deduct-requested and deduct-confirmed; Reversed covers a deduct that succeeded but was later refunded.</summary>
public enum WalletTxnStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Reversed = 3
}
