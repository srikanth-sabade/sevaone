using SevaOne.Domain.Common;

namespace SevaOne.Infrastructure.Connectors;

/// <summary>Every call fails with DepartmentNotIntegratedException — registered for departments SevaOne hasn't onboarded yet (e.g. commercial-taxes), so callers get a clear domain error instead of a null-reference or timeout.</summary>
public sealed class StubDepartmentConnector : IDepartmentConnector
{
    public StubDepartmentConnector(string departmentCode)
    {
        DepartmentCode = departmentCode;
    }

    public string DepartmentCode { get; }
    public bool IsIntegrated => false;

    public Task<DepartmentSessionHandle> OpenSessionAsync(CitizenContext citizen, CancellationToken ct) =>
        throw new DepartmentNotIntegratedException(DepartmentCode);

    public Task<IReadOnlyList<RemoteScheme>> ListSchemesAsync(DepartmentSessionHandle session, CancellationToken ct) =>
        throw new DepartmentNotIntegratedException(DepartmentCode);

    public Task<SubmitResult> SubmitAsync(DepartmentSessionHandle session, SubmitApplicationCommand cmd, CancellationToken ct) =>
        throw new DepartmentNotIntegratedException(DepartmentCode);

    public Task<RemoteStatus> GetStatusAsync(DepartmentSessionHandle session, string departmentRef, CancellationToken ct) =>
        throw new DepartmentNotIntegratedException(DepartmentCode);

    public Task CloseSessionAsync(DepartmentSessionHandle session, CancellationToken ct) => Task.CompletedTask;

    public Task<DigiLockerFetchResult> FetchDocumentAsync(DepartmentSessionHandle session, string documentType, CancellationToken ct) =>
        throw new DepartmentNotIntegratedException(DepartmentCode);

    public Task<PaymentResult> InitiatePaymentAsync(DepartmentSessionHandle session, PaymentRequest request, CancellationToken ct) =>
        throw new DepartmentNotIntegratedException(DepartmentCode);

    public Task<ESignResult> RequestESignAsync(DepartmentSessionHandle session, ESignRequest request, CancellationToken ct) =>
        throw new DepartmentNotIntegratedException(DepartmentCode);
}
