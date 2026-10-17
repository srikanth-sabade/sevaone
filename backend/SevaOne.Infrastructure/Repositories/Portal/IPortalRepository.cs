using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Portal;

/// <summary>Data access for the portal.* schema: citizens, kiosk operators, and the SevaOne JWS token.</summary>
public interface IPortalRepository
{
    Task<Citizen?> GetCitizenByIdAsync(Guid id, CancellationToken ct);
    Task<Citizen?> GetCitizenByWso2SubjectAsync(string wso2Subject, CancellationToken ct);
    Task<Citizen?> GetCitizenByUsernameAsync(string username, CancellationToken ct);
    Task<Citizen?> GetCitizenBySevaOneUserIdAsync(long sevaOneUserId, CancellationToken ct);

    /// <summary>Only matches a citizen whose mobile has actually been OTP-verified (mobile_verified_at IS NOT NULL) — see portal.sp_get_citizen_by_mobile.</summary>
    Task<Citizen?> GetCitizenByMobileAsync(string mobile, CancellationToken ct);

    /// <summary>Used to detect a duplicate-registration attempt: a real Aadhaar (via DigiLocker) that already belongs to a DIFFERENT SevaOne account. aadhaar_hash is only ever set from a verified DigiLocker fetch — see portal.sp_get_citizen_by_aadhaar_hash.</summary>
    Task<Citizen?> GetCitizenByAadhaarHashAsync(string aadhaarHash, CancellationToken ct);
    Task<Citizen> InsertCitizenAsync(Citizen citizen, CancellationToken ct);
    Task UpdateCitizenAsync(Citizen citizen, CancellationToken ct);

    /// <summary>Removes a duplicate demo-seed citizen row once its data/applications have been merged into a real logged-in citizen. Not a general-purpose citizen delete.</summary>
    Task DeleteCitizenAsync(Guid id, CancellationToken ct);

    Task<OperatorMaster?> GetOperatorByCodeAsync(string operatorCode, CancellationToken ct);
    Task<OperatorMaster?> GetOperatorByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Operator login only -- includes PasswordHash, unlike GetOperatorByCodeAsync/GetOperatorByIdAsync.</summary>
    Task<OperatorCredentials?> GetOperatorCredentialsByCodeAsync(string operatorCode, CancellationToken ct);

    /// <summary>Looks up a portal.operator_whitelist row -- proof a user_id is a real, registered operator, for the self-claim flow. Null if not on the roster.</summary>
    Task<OperatorWhitelistEntry?> GetOperatorWhitelistEntryAsync(long userId, CancellationToken ct);

    /// <summary>Creates the operator_master row for a self-claimed whitelist entry, linked to a real WSO2 IS account (wso2Subject) rather than a local password. Success is false (not an exception) if operatorId was already claimed -- see portal.sp_claim_operator.</summary>
    Task<(bool Success, string Message, Guid? Id)> ClaimOperatorAsync(
        Guid id, string operatorId, string mobileNo, DateTimeOffset mobileVerifiedAt, string wso2Subject, DateTimeOffset createdAt, CancellationToken ct);

    /// <summary>Gov-admin panel login lookup -- see portal.sp_get_gov_admin_by_username. No local password: gov admins authenticate against WSO2 IS only.</summary>
    Task<GovAdminCredentials?> GetGovAdminByUsernameAsync(string username, CancellationToken ct);

    /// <summary>Creates a not-yet-claimed gov-admin row (Superadmin's "create admin" action) -- wso2_subject/mobile stay NULL until GovAdminClaimService's first-time-login claim. See sp_create_gov_admin.</summary>
    Task<(bool Success, string Message, Guid? Id)> CreateGovAdminAsync(
        Guid id, string username, string displayName, string tier, string? assignedDistrict,
        string? assignedTaluk, string? assignedDepartment, DateTimeOffset createdAt, CancellationToken ct);

    /// <summary>Lists every gov-admin row (Superadmin/ChiefMinister's admin-management view). See sp_list_gov_admins.</summary>
    Task<IReadOnlyList<GovAdminSummary>> ListGovAdminsAsync(CancellationToken ct);

    /// <summary>Toggles a gov-admin row's is_active flag. See sp_set_gov_admin_active.</summary>
    Task<bool> SetGovAdminActiveAsync(Guid id, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<OperatorAdminSummary>> ListOperatorsForAdminAsync(string? search, bool? isActive, string? operatorType, CancellationToken ct);
    Task<bool> SetOperatorActiveAsync(Guid id, bool isActive, CancellationToken ct);

    /// <summary>Completes a gov-admin's first-time-login claim: sets wso2_subject/mobile_no/mobile_verified_at on an existing, not-yet-claimed row. See sp_claim_gov_admin.</summary>
    Task<(bool Success, string Message)> ClaimGovAdminAsync(
        string username, string wso2Subject, string mobileNo, DateTimeOffset mobileVerifiedAt, CancellationToken ct);

    Task InsertTokenAsync(SevaOneToken token, IUnitOfWork? uow, CancellationToken ct);
    Task<SevaOneToken?> GetTokenByHashAsync(string tokenHash, CancellationToken ct);
    Task<SevaOneToken?> GetTokenByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Atomic UPDATE ... WHERE revoked_at IS NULL, rows-affected == 1 means this call won the race. Replaces EF's xmin-based optimistic concurrency (see SevaOneToken doc comment).</summary>
    Task<bool> TryRedeemTokenAsync(Guid tokenId, DateTimeOffset redeemedAt, CancellationToken ct);
}
