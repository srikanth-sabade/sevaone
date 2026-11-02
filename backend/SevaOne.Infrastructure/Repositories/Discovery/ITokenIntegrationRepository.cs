namespace SevaOne.Infrastructure.Repositories.Discovery;

/// <summary>Data access for the admin-configurable token integration (discovery.token_integration*).
/// Separate from the operator-only Sakala setup on sevaone_service_master, which this never writes.</summary>
public interface ITokenIntegrationRepository
{
    Task<IReadOnlyList<TokenIntegrationTypeRow>> ListTypesAsync(CancellationToken ct);

    Task<IReadOnlyList<TokenIntegrationListRow>> ListServicesAsync(string? search, string? departmentCode, bool configuredOnly, bool citizenVisibleOnly, int limit, CancellationToken ct);

    /// <summary>Always returns the service when it exists; TypeKey is null when nothing is configured yet.</summary>
    Task<TokenIntegrationRow?> GetAsync(Guid serviceId, CancellationToken ct);

    Task UpsertAsync(TokenIntegrationWrite write, string updatedBy, CancellationToken ct);

    /// <summary>Sets the Seva Sindhu-side service id (sevaone_service_master.sevasindhu_service_id) that the
    /// "edcs_api" type calls generate/acknowledgment with. Same column the operator-only Sakala setup reads.</summary>
    Task SetSevasindhuServiceIdAsync(Guid serviceId, string? value, CancellationToken ct);

    Task<bool> DeleteAsync(Guid serviceId, CancellationToken ct);

    /// <summary>The enabled setup for a service (what an apply click asks), or null when the service has none or it is switched off.</summary>
    Task<TokenIntegrationActiveRow?> GetEnabledAsync(Guid serviceId, CancellationToken ct);

    /// <summary>Allocates the unique application reference and records the handoff. Returns the reference.</summary>
    Task<string> CreateAttemptAsync(TokenIntegrationAttemptWrite attempt, CancellationToken ct);

    // ---- acknowledgement API (contract URL 2) ----
    Task<TokenIntegrationAttemptRow?> GetAttemptAsync(string edcsApplicationRefNo, CancellationToken ct);

    Task<TokenIntegrationAckResult> ApplyAcknowledgementAsync(TokenIntegrationAckWrite write, CancellationToken ct);

    Task<TokenApplicationFlags> GetApplicationFlagsAsync(string edcsApplicationRefNo, Guid citizenId, CancellationToken ct);

    Task<TokenIntegrationFileRow?> GetAcknowledgementFileAsync(string edcsApplicationRefNo, Guid citizenId, CancellationToken ct);

    // ---- token check (the department verifies the token it received) ----
    Task<TokenVerifyInfoRow?> GetVerifyInfoAsync(string edcsApplicationRefNo, CancellationToken ct);

    /// <summary>Records the first successful check. True when the token had already been checked before.</summary>
    Task<bool> MarkTokenVerifiedAsync(Guid attemptId, CancellationToken ct);

    // ---- status check (contract URL 3) ----
    /// <summary>Null unless the application is acknowledged and its service has a status API.</summary>
    Task<TokenStatusTargetRow?> GetStatusTargetAsync(string edcsApplicationRefNo, CancellationToken ct);

    Task<string?> GetStatusSecretCiphertextAsync(Guid serviceId, CancellationToken ct);

    Task RecordStatusCheckAsync(Guid attemptId, string? statusText, string? error, CancellationToken ct);

    Task<IReadOnlyList<string>> ListStatusDueAsync(TimeSpan olderThan, int limit, CancellationToken ct);

    // ---- acknowledgements held by the Seva Sindhu API ("edcs_api" type) ----
    Task<IReadOnlyList<TokenAckDueRow>> ListAckDueAsync(TimeSpan olderThan, int limit, CancellationToken ct);

    Task RecordAckCheckAsync(Guid attemptId, CancellationToken ct);

    // ---- settings shared by every service of one type (single sign-on) ----
    Task<TokenProviderRow?> GetProviderAsync(string typeKey, CancellationToken ct);

    /// <summary>secretCiphertext: null keeps the stored secret, empty removes it, anything else replaces it.</summary>
    Task SaveProviderAsync(string typeKey, string settingsJson, string? secretCiphertext, string updatedBy, CancellationToken ct);

    /// <summary>Records where a started handoff ended up and whether it worked ('initiated' or 'failed').</summary>
    Task FinishAttemptAsync(Guid attemptId, string? landingUrl, string status, string? error, CancellationToken ct);

    // ---- department credentials for the acknowledgement API ----
    Task<TokenIntegrationCredentialRow?> GetCredentialAsync(string username, CancellationToken ct);
    Task TouchCredentialAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TokenIntegrationCredentialListRow>> ListCredentialsAsync(Guid departmentId, CancellationToken ct);
    Task<Guid> CreateCredentialAsync(Guid departmentId, string username, string passwordHash, string createdBy, CancellationToken ct);
    Task<bool> UpdateCredentialAsync(Guid id, Guid departmentId, string? passwordHash, bool? isActive, CancellationToken ct);
}

public sealed class TokenVerifyInfoRow
{
    public Guid AttemptId { get; set; }
    public string EdcsApplicationRefNo { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public long SevaOneUserId { get; set; }
    public bool AssistedMode { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsSandbox { get; set; }
    public string? TokenHash { get; set; }
    public bool TokenRevoked { get; set; }
    public string? CitizenName { get; set; }
    public string? CitizenMobile { get; set; }
    public string? CitizenDigilockerId { get; set; }
    public bool SendMobile { get; set; }
    public bool SendDigilockerId { get; set; }
    public string ServiceValue { get; set; } = "";
    public DateTimeOffset? TokenVerifiedAt { get; set; }
}

public sealed class TokenApplicationFlags
{
    public bool HasAcknowledgement { get; set; }
    public bool StatusCheckAvailable { get; set; }
}

public sealed class TokenStatusTargetRow
{
    public Guid AttemptId { get; set; }
    public string EdcsApplicationRefNo { get; set; } = "";
    public Guid CitizenId { get; set; }
    public string DepartmentApplicationRefNo { get; set; } = "";
    public string StatusUrl { get; set; } = "";
    public string? ExtraConfig { get; set; }
    public string? StatusSecretCiphertext { get; set; }
    public DateTimeOffset? StatusCheckedAt { get; set; }
    public string? DepartmentStatusText { get; set; }
    public Guid? ApplicationId { get; set; }
    public int? ApplicationStatus { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string TypeKey { get; set; } = "";
    public string? ProviderSettings { get; set; }
    public string? ProviderSecretCiphertext { get; set; }
    public Guid ServiceId { get; set; }
    public Guid DepartmentId { get; set; }
    public string ServiceName { get; set; } = "";
    public int SakalaDays { get; set; }
}

public sealed class TokenIntegrationAttemptRow
{
    public Guid AttemptId { get; set; }
    public string EdcsApplicationRefNo { get; set; } = "";
    public Guid ServiceId { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public Guid CitizenId { get; set; }
    public long SevaOneUserId { get; set; }
    public string? TokenHash { get; set; }
    public string ServiceName { get; set; } = "";
    public int SakalaDays { get; set; }
    public string[] AllowedIps { get; set; } = [];
    public string Status { get; set; } = "";
    public string? DepartmentApplicationRefNo { get; set; }
    public bool IsSandbox { get; set; }
}

public sealed class TokenIntegrationAckWrite
{
    public Guid AttemptId { get; set; }
    public string DepartmentApplicationRefNo { get; set; } = "";
    public string? PaymentStatus { get; set; }
    public decimal? Fee { get; set; }
    public DateTimeOffset? FeeCollectionDate { get; set; }
    public string? PaymentRefNumber { get; set; }
    public string? FileType { get; set; }
    public DateTimeOffset? AcknowledgmentDate { get; set; }
    public byte[]? Acknowledgment { get; set; }
    /// <summary>The call as received, JSON, without the acknowledgement file or the token.</summary>
    public string Payload { get; set; } = "{}";
    public string PayloadHash { get; set; } = "";
    public string? CallerIp { get; set; }
    public bool? TokenVerified { get; set; }
}

public sealed class TokenIntegrationAckResult
{
    public long UpdateId { get; set; }
    public bool AlreadyReceived { get; set; }
}

public sealed class TokenIntegrationFileRow
{
    public string? FileType { get; set; }
    public string FileName { get; set; } = "";
    public byte[] Content { get; set; } = [];
}

public sealed class TokenIntegrationCredentialRow
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class TokenIntegrationCredentialListRow
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset PasswordChangedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}

public sealed class TokenIntegrationActiveRow
{
    public string TypeKey { get; set; } = "";
    /// <summary>Null for single sign-on types: the address comes back from the other system for each citizen.</summary>
    public string? LandingUrl { get; set; }
    public bool SendMobile { get; set; }
    public bool SendDigilockerId { get; set; }
    public string? ExtraConfig { get; set; }
}

public sealed class TokenProviderRow
{
    public string TypeKey { get; set; } = "";
    public string SettingsJson { get; set; } = "{}";
    public string? SecretCiphertext { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "";
}

public sealed class TokenIntegrationAttemptWrite
{
    public Guid ServiceId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid CitizenId { get; set; }
    public long SevaOneUserId { get; set; }
    public Guid? TokenId { get; set; }
    public bool AssistedMode { get; set; }
    public string LandingUrl { get; set; } = "";
    /// <summary>JSON of what is posted to the department, without the token.</summary>
    public string SentFields { get; set; } = "{}";
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>A department's practice run: test citizen, EDCSTEST reference, never shown to a real citizen.</summary>
    public bool IsSandbox { get; set; }
    /// <summary>A reference issued by another system (the Seva Sindhu EP0115 API). Null: we allocate one.</summary>
    public string? EdcsApplicationRefNo { get; set; }
}

public sealed class TokenIntegrationTypeRow
{
    public string TypeKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public string FieldsJson { get; set; } = "{}";
}

public sealed class TokenIntegrationListRow
{
    public Guid ServiceId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string ServiceCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? NameKannada { get; set; }
    public bool CitizenVisible { get; set; }
    public string? OperatorMethod { get; set; }
    public string? TypeKey { get; set; }
    public bool? IsEnabled { get; set; }
    public string? LandingUrl { get; set; }
}

public sealed class TokenIntegrationRow
{
    public Guid ServiceId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string ServiceCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? OperatorMethod { get; set; }
    public string? OperatorSevasindhuId { get; set; }
    public string? TypeKey { get; set; }
    public string? LandingUrl { get; set; }
    public bool? SendMobile { get; set; }
    public bool? SendDigilockerId { get; set; }
    public string? StatusUrl { get; set; }
    public string[]? AllowedIps { get; set; }
    public string? ExtraConfig { get; set; }
    public bool HasStatusSecret { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public int? Version { get; set; }
}

public sealed class TokenIntegrationWrite
{
    public Guid ServiceId { get; set; }
    public string TypeKey { get; set; } = "";
    public string? LandingUrl { get; set; }
    public bool SendMobile { get; set; }
    public bool SendDigilockerId { get; set; }
    public string? StatusUrl { get; set; }
    public string[] AllowedIps { get; set; } = [];
    public string ExtraConfig { get; set; } = "{}";
    public bool IsEnabled { get; set; }

    /// <summary>Already encrypted. Null keeps the stored secret, empty clears it, anything else replaces it.</summary>
    public string? StatusSecretCiphertext { get; set; }
}

public sealed class TokenAckDueRow
{
    public Guid AttemptId { get; set; }
    public string EdcsApplicationRefNo { get; set; } = "";
    public long SevaOneUserId { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string? SevasindhuServiceId { get; set; }
}
