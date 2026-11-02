using System.Text.Json.Serialization;

namespace SevaOne.Business.Discovery.DTOs;

/// <summary>The body the department posts to the acknowledgement API (Seva Sindhu V3, URL 2). Property
/// names follow the contract; binding is case-insensitive. Dates stay strings because the contract
/// sends them without a time zone (read as Indian Standard Time).</summary>
public sealed class TokenAckRequest
{
    public string? UserId { get; set; }
    public string? DepartmentApplicationRefNo { get; set; }
    public string? EdcsApplicationRefNo { get; set; }
    public string? Token { get; set; }
    public string? DepartmentPaymentStatus { get; set; }
    public string? DepartmentFeeCollectionDate { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? DepartmentFee { get; set; }

    public string? DepartmentPaymentRefNumber { get; set; }
    public string? AcknowledgmentFileType { get; set; }
    public string? AcknowledgmentDate { get; set; }

    /// <summary>The acknowledgement document, base64.</summary>
    public string? Acknowledgment { get; set; }
}

/// <summary>Success reply, with the exact property names the contract shows.</summary>
public sealed record TokenAckSuccess(
    [property: JsonPropertyName("DepartmentApplicationUpdateId")] long DepartmentApplicationUpdateId,
    [property: JsonPropertyName("EdcsApplicationRefNo")] string EdcsApplicationRefNo,
    [property: JsonPropertyName("DepartmentApplicationRefNo")] string DepartmentApplicationRefNo);

/// <summary>Failure reply, with the exact property name the contract shows.</summary>
public sealed record TokenAckFailure([property: JsonPropertyName("Message")] string Message);

/// <summary>The practice (sandbox) version of the failure reply: the contract's message plus, for practice runs only, why.</summary>
public sealed record SandboxAckFailure(
    [property: JsonPropertyName("Message")] string Message,
    [property: JsonPropertyName("reason")] string? Reason);

/// <summary>Outcome of an acknowledgement. On failure Reason is for our own logs only; the department
/// is told just "Data Not Inserted", as the contract says.</summary>
public sealed record TokenAckOutcome(TokenAckSuccess? Success, string? Reason)
{
    public bool IsSuccess => Success is not null;
}

/// <summary>Who is calling the acknowledgement API, from its Basic-auth login.</summary>
public sealed record TokenIntegrationCaller(Guid CredentialId, Guid DepartmentId, string DepartmentCode);

public sealed record TokenIntegrationCredentialDto(
    Guid Id, string Username, bool IsActive, DateTimeOffset CreatedAt, string CreatedBy,
    DateTimeOffset PasswordChangedAt, DateTimeOffset? LastUsedAt);

/// <summary>Returned once, when a login is created or its password reset. The password cannot be read again.</summary>
public sealed record TokenIntegrationNewCredentialDto(Guid Id, string Username, string Password);

/// <summary>The acknowledgement document for a citizen's own application, ready to send as a download.</summary>
public sealed record TokenIntegrationFileDto(string ContentType, string FileName, byte[] Content);
