using SevaOne.Business.Portal.DTOs;
namespace SevaOne.Business.Portal.Interfaces;

public interface IPortalApplyService
{
    /// <summary>Citizen self-service path. Only requires ICurrentCitizen when the service
    /// actually has a real deep link needing a token (RequiresLogin:true otherwise-anonymous
    /// case) -- a plain homepage-fallback service resolves with no login at all.</summary>
    Task<ApplyOutcomeDto> CreateApplyTokenAsync(CreateApplyTokenRequest request, CancellationToken ct);

    /// <summary>Operator-assisted path — requires ICurrentOperator, always mints AssistedMode:true for the citizen identified by SevaOneUserId.</summary>
    Task<ApplyTokenDto> CreateAssistedApplyTokenAsync(CreateAssistedApplyTokenRequest request, CancellationToken ct);

    /// <summary>Department integration testing path — no citizen/operator session required at all, mints for a fixed seeded test citizen.</summary>
    Task<ApplyTokenDto> CreateTestApplyTokenAsync(CreateTestApplyTokenRequest request, CancellationToken ct);

    /// <summary>Resolves the department's return-redirect token (see PortalDeptFacingService.CompleteApplicationAsync) and cross-checks it against the current citizen session, if any.</summary>
    Task<ApplyReturnResult> CompleteReturnAsync(string encryptedToken, CancellationToken ct);
}
