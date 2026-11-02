using SevaOne.Domain.Enums;

namespace SevaOne.Business.Portal.DTOs;

public sealed record GetUserDetailsRequest(string DepartmentId, string Token);

/// <summary>
/// One redeem call, one response shape -- covers both the "already know this citizen" and
/// "first time seeing this citizen" cases a department used to have to pick between (formerly
/// Token Validate vs. Get User Details, two separate endpoints). The full profile is always
/// returned; a department that already has this citizen just ignores the fields it doesn't need.
/// </summary>
public sealed record UserDetailsResult(
    long SevaOneUserId,
    string Name,
    string? Mobile,
    string? Email,
    string? AadhaarMasked,
    DateOnly? DateOfBirth,
    string? Gender,
    string? StateName,
    string? DistrictName,
    string? Taluk,
    string? VillageName,
    string? Address,
    string ServiceId,
    bool AssistedMode,
    string? OperatorId,
    DateTimeOffset ExpiresAt,
    string? ReturnUrl);

public sealed record PostPaymentStatusRequest(
    string DepartmentId,
    string Token,
    string PaymentStatus,
    decimal? Amount,
    string? IdempotencyKey);

public sealed record PostPaymentStatusResult(bool Accepted, PaymentPostStatus Status);

/// <summary>
/// Department -> SevaOne: "this citizen's application at my department is done, send them
/// back." Deliberately NOT a redeem of the original apply-token (that one-time token was
/// already consumed at Token Validate / Get User Details time) -- this mints a second,
/// short-lived, one-time token whose only job is proving "the department really did
/// confirm completion for this specific citizen" when the browser lands back on SevaOne.
/// </summary>
public sealed record CompleteApplicationRequest(
    string DepartmentId,
    long SevaOneUserId,
    string ServiceId,
    string Status,
    string? DepartmentReference);

/// <summary>
/// Linked:false means the department has no SevaOne relationship for this citizen at all
/// (never went through an apply-token handoff, so SevaOne has no record to redirect back to)
/// -- NOT an error. Department integration code checks this flag and simply keeps the citizen
/// on its own portal rather than treating it as a failure to catch/retry.
/// </summary>
public sealed record CompleteApplicationResult(bool Linked, string? ReturnUrl, DateTimeOffset? ExpiresAt);
