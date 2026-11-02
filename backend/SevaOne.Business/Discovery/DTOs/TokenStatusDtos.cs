namespace SevaOne.Business.Discovery.DTOs;

/// <summary>How to call a department's status API and read its answer. The contract only says "a status-check
/// API using the GSC number", so this is set per service. Saved inside the integration's extra_config.
/// AuthType "provider" means the type's own sign-in (Kaveri). RequestField is the name of the one field we send; NumberSource is
/// which number goes in it: "department" (the department's reference from its acknowledgement) or "edcs" (our own application number).</summary>
public sealed record TokenStatusConfig(
    string AuthType = "none",
    string? AuthUser = null,
    string? AuthHeaderName = null,
    string StatusPath = "status",
    string? MessagePath = null,
    IReadOnlyDictionary<string, string>? StatusMap = null,
    string RequestField = "GscNumber",
    string NumberSource = "department");

/// <summary>What the admin "test" shows: the department's raw answer and how we read it.</summary>
public sealed record TokenStatusTestResult(
    int? HttpStatus, string? RawBody, string? DepartmentStatus, string? Message, string? MappedStatus, string? Error);

/// <summary>Result of asking the department about one of a citizen's applications.</summary>
public sealed record TokenStatusRefreshDto(bool Checked, string? DepartmentStatus, string? Message, bool Failed);

/// <summary>Settings for the Kaveri 2.0 integration, shared by every Kaveri service. The password is kept separately, encrypted.</summary>
public sealed record KaveriSettings(
    string LoginUrl = "", string RefreshUrl = "", string SsoUrl = "", string StatusUrl = "", string Username = "",
    bool AssumeEmailVerified = false);

public sealed record TokenProviderDto(string TypeKey, KaveriSettings? Kaveri, bool HasSecret, bool IsReady, DateTimeOffset? UpdatedAt, string? UpdatedBy);

public sealed record TokenProviderWriteRequest(KaveriSettings Kaveri, string? Password = null);

public sealed record TokenProviderTestResult(bool Ok, string Message);
