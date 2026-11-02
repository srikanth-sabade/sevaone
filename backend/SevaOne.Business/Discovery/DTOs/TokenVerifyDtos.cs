using System.Text.Json.Serialization;

namespace SevaOne.Business.Discovery.DTOs;

/// <summary>What the department sends to check the token it received with the citizen.</summary>
public sealed class TokenVerifyRequest
{
    public string? Token { get; set; }
    public string? EdcsApplicationRefNo { get; set; }
}

/// <summary>The answer. When Valid is false only Reason (and Sandbox) are set. Property names are fixed: departments code against them.</summary>
public sealed record TokenVerifyResult(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("reason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Reason = null,
    [property: JsonPropertyName("userId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? UserId = null,
    [property: JsonPropertyName("edcsApplicationRefNo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? EdcsApplicationRefNo = null,
    [property: JsonPropertyName("serviceId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ServiceId = null,
    [property: JsonPropertyName("assistedMode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? AssistedMode = null,
    [property: JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name = null,
    [property: JsonPropertyName("mobile"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Mobile = null,
    [property: JsonPropertyName("digilockerId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DigilockerId = null,
    [property: JsonPropertyName("expiresAt"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? ExpiresAt = null,
    [property: JsonPropertyName("alreadyVerified"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? AlreadyVerified = null,
    [property: JsonPropertyName("sandbox")] bool Sandbox = false);

/// <summary>Start of a practice run: a sandbox application and the exact fields SevaOne would post to the department's landing page.</summary>
public sealed record SandboxStartDto(
    string EdcsApplicationRefNo, string ServiceCode, string DepartmentServiceId, DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> Fields, string Note);

public sealed class SandboxStartRequest
{
    public string? ServiceCode { get; set; }
}

public sealed class LandingTestRequest
{
    public string? Url { get; set; }
    public string? ServiceCode { get; set; }
}

public sealed record LandingTestResult(
    string EdcsApplicationRefNo, int? HttpStatus, bool Ok, long ElapsedMs, string? Error, IReadOnlyList<string> FieldsSent, string Note);

public sealed class StatusTestRequest
{
    public string? Url { get; set; }
    public string? RequestField { get; set; }
    public string? Number { get; set; }
    public string? StatusPath { get; set; }
    public string? MessagePath { get; set; }
    public string? AuthType { get; set; }
    public string? AuthUser { get; set; }
    public string? AuthHeaderName { get; set; }
    public string? Secret { get; set; }
}
