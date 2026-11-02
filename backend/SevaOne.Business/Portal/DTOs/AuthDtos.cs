using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Business.Portal.DTOs;

public sealed record CompleteLoginResult(Wso2CitizenTokens Tokens, string ReturnTo);

/// <summary>flow is "signin" or "signup" (signup sets CeG DLFLow). PendingId is the value returned by mobile/verify when found:false, so the OTP-verified mobile is carried through DigiLocker (CeG does not return it).</summary>
public sealed record StartDigiLockerLoginRequest(string Flow, string? ReturnTo, string? PendingId);
public sealed record StartDigiLockerLoginResult(string RedirectUrl);

public sealed record CompleteDigiLockerLoginRequest(string TxnId, string AppParam, string? Status);

public sealed record StartMobileOtpRequest(string Mobile);
public sealed record StartMobileOtpResult(string ChallengeId);

/// <summary>Backs GET /api/v1/me/audit -- the citizen-facing "Recent Activity" feed on the
/// dashboard. Named after the underlying audit.audit_logs table/AuditLog model it reads from
/// (same convention as everywhere else in this codebase), even though the UI label is
/// "Recent Activity" -- the two names serve different audiences (API/DB concept vs. what a
/// citizen sees) and don't need to match.</summary>
public sealed record AuditEntryDto(Guid Id, string Action, string Resource, string? Detail, DateTimeOffset At);

public sealed record VerifyMobileOtpRequest(string ChallengeId, string Otp);

/// <summary>
/// Found:false means no citizen has this VERIFIED mobile on file yet -- not an error, this is
/// the normal "new citizen" branch. The frontend should proceed to the DigiLocker flow next.
/// Found:true means the OTP matched an existing citizen and Tokens/ReturnTo are the established
/// login credential, exactly like any other login result.
/// </summary>
public sealed record VerifyMobileOtpResult(bool Found, string Mobile, Wso2CitizenTokens? Tokens, string? ReturnTo, string? DigiLockerPendingId = null);

/// <summary>AccessToken is read from the seva.wso2.at cookie by the controller — logout revokes it against WSO2 and purges its introspection-cache entry, best-effort, even if it's already expired/invalid.</summary>
public sealed record LogoutRequest(string? AccessToken);

/// <summary>AadhaarVerified is true only when AadhaarMasked came from DigiLocker's real Aadhaar document, not a self-typed registration-form value — the frontend should hide/pre-fill the Aadhaar-last-4 field at registration when this is true instead of asking again.</summary>
public sealed record MeDto(
    Guid Id,
    string SevaId,
    string Name,
    string Username,
    string? Mobile,
    string? Email,
    string? Address,
    string? District,
    string? Taluk,
    string? StateName,
    string? VillageName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? AadhaarMasked,
    bool AadhaarVerified,
    string? NameKannada,
    string? AddressKannada,
    string? DistrictKannada,
    string? StateKannada,
    string? VillageKannada,
    bool ProfileCompleted,
    int ProfileFinishPct);

public sealed record DigiLockerIssuedDocumentDto(
    string DocumentName,
    string DocumentType,
    string DocumentUri,
    string DocumentDescription,
    string DocumentIssuer,
    string DocumentUploadDate);

public sealed record UpdateProfileRequest(
    string Name,
    string Mobile,
    string? Email,
    string Address,
    string District,
    string Taluk,
    string? StateName,
    string? VillageName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? AadhaarLast4);
