namespace SevaOne.Business.Discovery.DTOs;

public sealed record CatalogRoleDto(string RoleKey, string DisplayName, string? Description);

public sealed record ServiceVisibilityDto(string RoleKey, string DisplayName, DateTimeOffset GrantedAt, string? GrantedBy);

public sealed record BulkVisibilityRequest(string? DepartmentCode, string? SubDepartmentName, IReadOnlyList<string> RoleKeys, bool Grant);

public sealed record BulkVisibilityResult(int MatchedServices, int Applied);

public sealed record ServiceChangeLogDto(
    Guid Id, string? ServiceCode, string? ServiceName, Guid? BatchId,
    DateTimeOffset ChangedAt, string ChangedBy, string ChangeType, string? Diff);
