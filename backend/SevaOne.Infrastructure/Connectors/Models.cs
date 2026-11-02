namespace SevaOne.Infrastructure.Connectors;

public sealed record CitizenContext(
    Guid CitizenId,
    string Wso2Subject,
    string Name,
    string? Mobile,
    string? AadhaarMasked,
    string SevaId);

public sealed record DepartmentSessionHandle(
    string DepartmentCode,
    string AccessToken,
    DateTimeOffset ExpiresAt);

public sealed record RemoteScheme(string ServiceCode, string Name, string Status);

public sealed record SubmitApplicationCommand(
    string ServiceCode,
    string ApplicantName,
    string Address,
    string? Purpose,
    string? District,
    string? Taluk);

public sealed record SubmitResult(string DepartmentReference, string Status, string Note);

public sealed record RemoteStatus(string DepartmentReference, string Status, string Note);

public sealed record DigiLockerFetchResult(string DocumentType, string DocumentId, bool Found, string Note);

public sealed record PaymentRequest(string ServiceCode, decimal AmountInr, string Purpose);

public sealed record PaymentResult(string PaymentId, string Gateway, string Status, string Note);

public sealed record ESignRequest(string DocumentId, string SignerName);

public sealed record ESignResult(string RequestId, string Status, string Note);
