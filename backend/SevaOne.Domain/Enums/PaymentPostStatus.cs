namespace SevaOne.Domain.Enums;

/// <summary>Department fee payment status, as POSTed by the department.</summary>
public enum PaymentPostStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Cancelled = 3
}
