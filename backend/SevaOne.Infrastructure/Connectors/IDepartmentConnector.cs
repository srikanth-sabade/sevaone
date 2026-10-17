namespace SevaOne.Infrastructure.Connectors;

/// <summary>One HTTP integration per department, or a stub that refuses every call for a department SevaOne hasn't onboarded yet.</summary>
public interface IDepartmentConnector
{
    string DepartmentCode { get; }
    bool IsIntegrated { get; }

    Task<DepartmentSessionHandle> OpenSessionAsync(CitizenContext citizen, CancellationToken ct);
    Task<IReadOnlyList<RemoteScheme>> ListSchemesAsync(DepartmentSessionHandle session, CancellationToken ct);
    Task<SubmitResult> SubmitAsync(DepartmentSessionHandle session, SubmitApplicationCommand cmd, CancellationToken ct);
    Task<RemoteStatus> GetStatusAsync(DepartmentSessionHandle session, string departmentRef, CancellationToken ct);
    Task CloseSessionAsync(DepartmentSessionHandle session, CancellationToken ct);

    Task<DigiLockerFetchResult> FetchDocumentAsync(DepartmentSessionHandle session, string documentType, CancellationToken ct);
    Task<PaymentResult> InitiatePaymentAsync(DepartmentSessionHandle session, PaymentRequest request, CancellationToken ct);
    Task<ESignResult> RequestESignAsync(DepartmentSessionHandle session, ESignRequest request, CancellationToken ct);
}

public interface IDepartmentConnectorRegistry
{
    IDepartmentConnector Get(string departmentCode);
    IReadOnlyCollection<IDepartmentConnector> All { get; }
}
