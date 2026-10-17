namespace SevaOne.Business.Discovery.DTOs;

/// <summary>
/// Backs the redesigned /catalog page (department grid -> sub-department rail -> service
/// detail) -- deliberately separate from DepartmentDto/SchemeSummaryDto/SchemeDetailDto,
/// which stay flat/no-sub-department to avoid changing the shape Departments.tsx/Services.tsx
/// already depend on.
/// </summary>
public sealed record CatalogHierarchyDepartmentDto(
    string Code,
    string Name,
    string? DepartmentUrl,
    string? DepartmentUrlKannada,
    int SubDepartmentCount,
    int ServiceCount);

public sealed record CatalogHierarchySubDepartmentDto(string Name, int ServiceCount);

public sealed record CatalogHierarchyServiceDto(
    string ServiceCode,
    /// <summary>{departmentCode}-{serviceCode} -- globally unique, matches the shape catalogHierarchy.ts's frontend helpers already use; never persisted, computed here.</summary>
    string CompositeCode,
    string? SubDepartmentName,
    string Name,
    string? NameKannada,
    string? ShortDescription,
    string? Description,
    string? DescriptionKannada,
    bool? IsSakalaEnabled,
    string? SakalaLabelRaw,
    int SakalaDays,
    decimal? DepartmentFee,
    decimal? ServiceCharge,
    IReadOnlyList<string> DocumentsEn,
    IReadOnlyList<string> DocumentsKn,
    IReadOnlyList<string> EligibilityEn,
    IReadOnlyList<string> EligibilityKn,
    IReadOnlyList<string> ProcedureEn,
    IReadOnlyList<string> ProcedureKn,
    string? Jurisdiction,
    string? Category,
    string? ModeOfService,
    string? ApplyUrl,
    string? ApplyUrlKannada,
    string? ServiceUrl,
    /// <summary>'ajsk' / 'sakala_token' / null -- see Scheme.IntegrationMethod. Frontend uses this
    /// to decide whether operator-assisted Apply should mint the normal deep-link token or go
    /// through the special redirect; citizen Apply never reads it.</summary>
    string? IntegrationMethod);

public sealed record CatalogSearchServiceDto(
    string DepartmentCode, string DepartmentName, string ServiceCode, string CompositeCode,
    string? SubDepartmentName, string Name, string? NameKannada, string? ShortDescription, int SakalaDays);

public sealed record CatalogSearchResultDto(
    IReadOnlyList<CatalogHierarchyDepartmentDto> Departments,
    IReadOnlyList<CatalogSearchServiceDto> Services);

public sealed record CatalogDepartmentDetailDto(
    string Code,
    string Name,
    string? DepartmentUrl,
    string? DepartmentUrlKannada,
    IReadOnlyList<CatalogHierarchySubDepartmentDto> SubDepartments,
    IReadOnlyList<CatalogHierarchyServiceDto> Services);

/// <summary>
/// One row per citizen-visible service, department context included -- the shape the AI
/// backend's ingestion (see ai_backend/app/ingestion/) reads to build its own knowledge base
/// from, instead of the Excel/website sources it uses today. GET /api/v1/catalog/ai-export.
/// </summary>
public sealed record AiCatalogExportServiceDto(
    string DepartmentCode,
    string DepartmentName,
    string? SubDepartmentName,
    string ServiceCode,
    /// <summary>{departmentCode}-{serviceCode} -- globally unique, same convention as CatalogHierarchyServiceDto.CompositeCode.</summary>
    string CompositeCode,
    string Name,
    string? NameKannada,
    string? ShortDescription,
    string? Description,
    string? DescriptionKannada,
    int SakalaDays,
    decimal? DepartmentFee,
    decimal? ServiceCharge,
    IReadOnlyList<string> DocumentsEn,
    IReadOnlyList<string> DocumentsKn,
    IReadOnlyList<string> EligibilityEn,
    IReadOnlyList<string> EligibilityKn,
    IReadOnlyList<string> ProcedureEn,
    IReadOnlyList<string> ProcedureKn,
    IReadOnlyList<string> Aliases,
    string? Jurisdiction,
    string? Category,
    string? ModeOfService,
    string? ApplyUrl,
    string? ApplyUrlKannada,
    string? ServiceUrl);
