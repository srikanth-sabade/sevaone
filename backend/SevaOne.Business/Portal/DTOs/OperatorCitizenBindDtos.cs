namespace SevaOne.Business.Portal.DTOs;

/// <summary>
/// Assisted-mode citizen binding -- a deliberately separate OTP mechanism from the citizen's own
/// mobile-OTP login (AuthController's auth/mobile/start+verify). An operator is binding a citizen
/// they're standing next to at a counter, not authenticating themselves as that citizen, so this
/// never touches ICitizenSessionService/Wso2 tokens and never signs the citizen in.
/// </summary>
public sealed record SendOperatorCitizenOtpRequest(string Mobile);

public sealed record SendOperatorCitizenOtpResult(string ChallengeId);

public sealed record VerifyOperatorCitizenOtpRequest(string ChallengeId, string Otp);

/// <summary>SevaOneUserId (not the internal Guid Id) -- the same externally-facing identifier already used everywhere else a citizen is exposed outside the backend (Apply URLs, JWS claims).</summary>
public sealed record BoundCitizenDto(long SevaOneUserId, string Name, string? Mobile);

public sealed record OperatorMeResult(string OperatorCode, string? OperatorName, BoundCitizenDto? BoundCitizen);
