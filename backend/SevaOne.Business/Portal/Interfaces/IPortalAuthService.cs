using SevaOne.Business.Portal.DTOs;
namespace SevaOne.Business.Portal.Interfaces;

public interface IPortalAuthService
{
    Task<StartDigiLockerLoginResult> StartDigiLockerLoginAsync(StartDigiLockerLoginRequest request, CancellationToken ct);
    Task<CompleteLoginResult> CompleteDigiLockerLoginAsync(CompleteDigiLockerLoginRequest request, CancellationToken ct);
    /// <summary>The citizen's documents as stored in SevaOne -- works after the DigiLocker session has ended.</summary>
    Task<IReadOnlyList<DigiLockerDocumentDto>> ListDigiLockerDocumentsAsync(CancellationToken ct);
    /// <summary>Pulls the document list and every available file from DigiLocker into object storage. Needs a live DigiLocker session at the gateway.</summary>
    Task<IReadOnlyList<DigiLockerDocumentDto>> SyncDigiLockerDocumentsAsync(CancellationToken ct);
    Task<DigiLockerDocumentFile> GetDigiLockerDocumentFileAsync(Guid documentId, string format, CancellationToken ct);

    /// <summary>Sends an OTP to the given mobile number. Doesn't reveal whether the number is already registered — that's only surfaced after VerifyMobileLoginAsync confirms ownership.</summary>
    Task<StartMobileOtpResult> StartMobileLoginAsync(StartMobileOtpRequest request, CancellationToken ct);

    /// <summary>Verifies the OTP, then either establishes a session (existing citizen) or reports Found:false so the caller can proceed to DigiLocker (new citizen).</summary>
    Task<VerifyMobileOtpResult> VerifyMobileLoginAsync(VerifyMobileOtpRequest request, CancellationToken ct);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct);
    Task<MeDto> GetMeAsync(CancellationToken ct);
    Task<MeDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct);

    /// <summary>Most-recent-first, capped at `take`. Everything this citizen has done that
    /// writes an audit entry -- apply clicks (see PortalApplyService), logins, profile updates,
    /// etc. -- across the whole system, not scoped to one feature.</summary>
    Task<IReadOnlyList<AuditEntryDto>> GetMyAuditAsync(int take, CancellationToken ct);
}
