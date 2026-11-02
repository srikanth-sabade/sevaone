namespace SevaOne.Business.Orchestration.DTOs;

/// <summary>DepartmentCode is required -- service_id is only unique within a department (see
/// V39), so resolving the scheme by ServiceCode alone risks matching another department's
/// same-numbered service.</summary>
public sealed record SubmitApplicationRequest(
    string DepartmentCode,
    string ServiceCode,
    string ApplicantName,
    string Address,
    string? Purpose,
    string? District,
    string? Taluk);
