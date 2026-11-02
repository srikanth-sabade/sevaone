namespace SevaOne.Business.Discovery.DTOs;

public sealed record DepartmentDto(
    Guid Id,
    string Code,
    string Name,
    string PortalName,
    string PortalDomain,
    bool IsIntegrated,
    int SchemeCount);

public sealed record ListSchemesRequest(string? DepartmentCode, string? Q, string? Category);

public sealed record SchemeSummaryDto(
    string ServiceCode,
    string Name,
    string DepartmentCode,
    string DepartmentName,
    string Category,
    int SakalaDays,
    decimal? DepartmentFee,
    decimal? ServiceCharge,
    string Description,
    IReadOnlyList<string> Eligibility,
    IReadOnlyList<string> Aliases,
    bool DepartmentIntegrated,
    // Only meaningful from ListPopularSchemesAsync (backed by sp_list_top_requested_services,
    // which always returns a curated-fallback row even at zero real usage) - every other
    // caller of this shared DTO leaves these at 0. The frontend uses TotalApplyCount, not row
    // presence, to tell a genuinely-requested service apart from a fallback one.
    // CatalogueApplyCount + AiApplyCount always sum to TotalApplyCount.
    long CatalogueApplyCount = 0,
    long AiApplyCount = 0,
    long TotalApplyCount = 0);

public sealed record GetSchemeRequest(string ServiceCode);

public sealed record SchemeDetailDto(
    string ServiceCode,
    string Name,
    string DepartmentCode,
    string DepartmentName,
    string Category,
    int SakalaDays,
    decimal? DepartmentFee,
    decimal? ServiceCharge,
    string Description,
    IReadOnlyList<string> Documents,
    IReadOnlyList<string> Eligibility,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Steps,
    string? Portal,
    bool DepartmentIntegrated);

/// <summary>AI-chat search stand-in: matches against the SevaOne master and, when more than one service matches, returns a clarifying question instead of the full match list. Deterministic keyword match today — swap the matching/question generation for a real LLM later without touching the response shape.</summary>
public sealed record SearchServicesRequest(string Q);

public sealed record ServiceSearchResultDto(string Kind, SchemeSummaryDto? Service, ClarifyDto? Clarify);

public sealed record ClarifyDto(string Question, IReadOnlyList<ClarifyOptionDto> Options);

public sealed record ClarifyOptionDto(string Category, string DepartmentName, int MatchCount);
