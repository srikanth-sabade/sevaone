namespace SevaOne.Business.Portal.DTOs;

public sealed record GovAdminLoginRequest(string Username, string Password);
public sealed record GovAdminLoginResult(string SessionId, string DisplayName, string Tier, TimeSpan Ttl);
public sealed record GovAdminLogoutRequest(string? SessionId);
public sealed record GovAdminMeResult(string DisplayName, string Tier, string? AssignedDistrict, string? AssignedTaluk, string? AssignedDepartment);

/// <summary>Gov-admin first-time-login claim -- mirrors OperatorClaimStartRequest's shape
/// exactly (username instead of a numeric user_id, since Superadmin picks a real username at
/// creation time rather than referencing an external roster). Password is collected up front,
/// same rationale as the operator claim: there's nowhere else for a credential to live once the
/// OTP verifies.</summary>
public sealed record GovAdminClaimStartRequest(string Username, string Mobile, string Password);
public sealed record GovAdminClaimStartResult(string ChallengeId);

public sealed record GovAdminClaimVerifyRequest(string ChallengeId, string Otp);
public sealed record GovAdminClaimVerifyResult(string Username);

/// <summary>Superadmin's "create admin" action. District/taluk/department are validated against
/// the tier (see GovAdminManagementService) before hitting the DB CHECK constraint.</summary>
public sealed record CreateGovAdminRequest(string Username, string DisplayName, string Tier, string? AssignedDistrict, string? AssignedTaluk, string? AssignedDepartment);
public sealed record CreateGovAdminResult(Guid Id, string Username);

public sealed record GovAdminListItem(
    Guid Id, string Username, string DisplayName, string Tier, string? AssignedDistrict,
    string? AssignedTaluk, string? AssignedDepartment, bool IsActive, bool Claimed, DateTimeOffset CreatedAt);

public sealed record GovAdminSessionDto(
    string SessionId, Guid GovAdminId, string DisplayName, string Tier,
    DateTimeOffset ExpiresAt, bool IsCurrent);

public sealed record OperatorAdminListItem(
    Guid Id, string OperatorCode, string OperatorType, string? OperatorName,
    string? MobileNo, bool IsClaimed, bool IsActive, DateTimeOffset CreatedAt);

public sealed record GovAdminImportRowRequest(
    string Username, string DisplayName, string Tier, string? AssignedDistrict,
    string? AssignedTaluk, string? AssignedDepartment);

public sealed record GovAdminImportBatchDto(
    Guid Id, string SourceFilename, string SourceFormat, string UploadedBy,
    DateTimeOffset UploadedAt, string Status, int RowCount, int ErrorCount,
    int WarningCount, DateTimeOffset? PublishedAt, string? PublishedBy);

public sealed record GovAdminImportRowDto(
    Guid Id, int RowNumber, string? Action, IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings, GovAdminImportRowRequest? Data);

public sealed record GovAdminImportPublishResult(int Created, int Failed);

public sealed record SaveServiceNoticeRequest(
    string Title, string Message, string Severity, bool IsActive,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);

public sealed record ServiceNoticeDto(
    string Title, string Message, string Severity, bool IsActive,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, DateTimeOffset UpdatedAt);

public sealed record PublicServiceNoticeDto(
    string Title, string Message, string Severity,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, DateTimeOffset UpdatedAt);

public sealed record ServiceNoticeHistoryDto(
    Guid Id, string Title, string Message, string Severity, bool WasActive,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, DateTimeOffset SavedAt);

public sealed record CreateGovNotificationRequest(string Title, string Body);
public sealed record GovNotificationDto(
    Guid Id, string Title, string Body, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record PublicGovNotificationDto(Guid Id, string Title, string Body, DateTimeOffset PublishedAt);

public sealed record GovAdminActivityLogDto(
    Guid Id, string ActorUsername, string ActorDisplayName, string Action, string? Target, string? Detail, DateTimeOffset At);
