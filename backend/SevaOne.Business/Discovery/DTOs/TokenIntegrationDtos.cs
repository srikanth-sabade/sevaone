namespace SevaOne.Business.Discovery.DTOs;

public sealed record TokenIntegrationTypeDto(string TypeKey, string DisplayName, string? Description, string FieldsJson);

public sealed record TokenIntegrationListItemDto(
    Guid ServiceId, string DepartmentCode, string DepartmentName, string ServiceCode, string Name, string? NameKannada,
    bool CitizenVisible, string? OperatorMethod, string? TypeKey, bool IsConfigured, bool IsEnabled, string? LandingUrl);

/// <summary>One service with its token-integration setup. OperatorMethod / OperatorSevasindhuId are the
/// operator-only Sakala setup, shown read-only so the two lanes are never confused.</summary>
public sealed record TokenIntegrationDto(
    Guid ServiceId, string DepartmentCode, string DepartmentName, string ServiceCode, string Name,
    string? OperatorMethod, string? OperatorSevasindhuId,
    bool IsConfigured, string? TypeKey, string? LandingUrl, bool SendMobile, bool SendDigilockerId,
    string? StatusUrl, TokenStatusConfig StatusConfig, bool HasStatusSecret, long? KaveriServiceId, bool ProviderReady,
    IReadOnlyList<string> AllowedIps, bool IsEnabled,
    DateTimeOffset? UpdatedAt, string? UpdatedBy, int? Version, IReadOnlyList<string> Warnings,
    // Only for TypeKey "edcs_api": the optional Key1..Key5 fields a department's generate call may need.
    string? EdcsKey1 = null, string? EdcsKey2 = null, string? EdcsKey3 = null, string? EdcsKey4 = null, string? EdcsKey5 = null);

public sealed record TokenIntegrationWriteRequest(
    string TypeKey, string? LandingUrl, bool SendMobile, bool SendDigilockerId,
    string? StatusUrl, IReadOnlyList<string>? AllowedIps, bool IsEnabled,
    TokenStatusConfig? StatusConfig = null, string? StatusSecret = null, long? KaveriServiceId = null,
    // Only for TypeKey "edcs_api": the Seva Sindhu-side service id (e.g. 2952) its generate call needs.
    string? SevasindhuServiceId = null,
    // Only for TypeKey "edcs_api": optional Key1..Key5 some departments need (e.g. an Aadhaar hash). Usually blank.
    string? EdcsKey1 = null, string? EdcsKey2 = null, string? EdcsKey3 = null, string? EdcsKey4 = null, string? EdcsKey5 = null);
