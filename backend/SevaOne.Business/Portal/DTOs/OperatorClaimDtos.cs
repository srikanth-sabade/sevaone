namespace SevaOne.Business.Portal.DTOs;

/// <summary>Operator enters their existing SevaSindhu user_id (checked against
/// portal.operator_whitelist), a mobile number, and their chosen password -- mirrors the
/// shape of the citizen mobile-OTP flow (StartMobileOtpRequest), but password is collected
/// up front since operator credentials don't exist anywhere yet to fall back on.</summary>
public sealed record OperatorClaimStartRequest(long UserId, string Mobile, string Password);
public sealed record OperatorClaimStartResult(string ChallengeId);

public sealed record OperatorClaimVerifyRequest(string ChallengeId, string Otp);

/// <summary>OperatorCode is the claimed user_id, stringified -- the value this operator now
/// logs in with via OperatorController.Login, same as any pre-seeded operator.</summary>
public sealed record OperatorClaimVerifyResult(string OperatorCode);
