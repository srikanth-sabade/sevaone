using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Discovery;

/// <summary>Data access for the discovery.* schema: department catalog, scheme catalog, and per-scheme document URLs.</summary>
public interface IDiscoveryRepository
{
    Task<IReadOnlyList<Department>> ListDepartmentsAsync(CancellationToken ct);
    Task<IReadOnlyList<DepartmentWithSchemeCount>> ListDepartmentsWithSchemeCountAsync(CancellationToken ct);
    Task<Department?> GetDepartmentByIdAsync(Guid id, CancellationToken ct);
    Task<Department?> GetDepartmentByCodeAsync(string code, CancellationToken ct);

    Task<IReadOnlyList<Scheme>> ListSchemesAsync(Guid? departmentId, CancellationToken ct);
    Task<Scheme?> GetSchemeByIdAsync(Guid id, CancellationToken ct);
    Task<Scheme?> GetSchemeByCodeAsync(Guid departmentId, string serviceCode, CancellationToken ct);

    /// <summary>Service codes are globally unique in the seed catalogue even though the DB's uniqueness constraint is scoped to (department_id, service_id) — used by the citizen-facing apply flow, which knows only the code, not the department yet.</summary>
    /// <summary>service_id is only unique within a department, not globally (see V39) -- every
    /// caller must supply the department it already has, never look up by service code alone.</summary>
    Task<Scheme?> GetSchemeByDepartmentAndServiceCodeAsync(string departmentCode, string serviceCode, CancellationToken ct);

    /// <summary>Looks up an active scheme by its display NAME (case/whitespace-insensitive exact
    /// match) rather than any code - the only reliable link between this catalog and
    /// sevaone-rag's fully separate one. See sp_find_active_scheme_by_name.sql and
    /// ApplyClickTrackingService.ResolveAndTrackAiApplyAsync. departmentCode, when given, is a
    /// disambiguating hint (preferred, not required) for when the same name exists in more than
    /// one department.</summary>
    Task<ActiveSchemeByName?> FindActiveSchemeByNameAsync(string serviceName, string? departmentCode, string? operatorHint, CancellationToken ct);

    /// <summary>Scheme rows joined with their owning department — the catalog/search DTOs need department code/name/integrated in the same row, so this avoids an N+1 lookup per scheme.</summary>
    Task<IReadOnlyList<SchemeWithDepartment>> ListSchemesWithDepartmentAsync(string? departmentCode, string? category, bool onlyActive, CancellationToken ct);

    /// <summary>Citizen-visible, active services ranked by real apply attempts since `since`,
    /// most-requested first; zero-apply services fill any remaining slots (ordered by name) so
    /// this always returns `limit` rows. Backs the landing page's "Frequently requested"
    /// section -- see sp_list_top_requested_services.sql for why. Row shape matches
    /// SchemeWithDepartment (the query's extra ApplyCount column is simply unmapped/ignored).</summary>
    Task<IReadOnlyList<SchemeWithDepartment>> ListTopRequestedServicesAsync(DateTimeOffset since, int limit, CancellationToken ct);

    Task<SchemeWithDepartment?> GetSchemeWithDepartmentByServiceCodeAsync(string serviceCode, CancellationToken ct);

    Task<IReadOnlyList<SevaOneServiceDocument>> GetDocumentsBySchemeAsync(Guid schemeId, CancellationToken ct);

    Task<IReadOnlyList<SubDepartment>> ListSubDepartmentsByDepartmentAsync(Guid departmentId, CancellationToken ct);

    Task<CatalogAdmin?> GetCatalogAdminByCodeAsync(string adminCode, CancellationToken ct);

    // ---- Service Catalogue: write path (Postgres functions ishuk's fc056cc flagged as missing) ----

    /// <summary>Upserts by the department's business code. Only Name/DepartmentUrl(Kannada) are overwritten on conflict — portal_name/portal_domain/connector_type/is_integrated are curated separately and must never be blanked by ingestion.</summary>
    Task<Guid> UpsertDepartmentAsync(Guid id, string code, string name, string? departmentUrl, string? departmentUrlKannada, IUnitOfWork? uow, CancellationToken ct);

    /// <summary>Upserts by (DepartmentId, Name). DisplayOrder is intentionally not a parameter — admin-controlled, untouched by re-imports.</summary>
    Task<Guid> UpsertSubDepartmentAsync(Guid id, Guid departmentId, string name, IUnitOfWork? uow, CancellationToken ct);

    /// <summary>Upserts by (DepartmentId, ServiceCode). Leaves Fee/DocumentsJson/AliasesJson/Portal untouched — see ServiceUpsertRequest.</summary>
    Task<Guid> UpsertServiceAsync(ServiceUpsertRequest request, IUnitOfWork? uow, CancellationToken ct);

    /// <summary>Upserts by (SchemeId, DocumentUrl).</summary>
    Task<Guid> UpsertServiceDocumentAsync(Guid id, Guid schemeId, string? documentName, string? documentNameKannada, string documentUrl, string? documentType, bool isMandatory, IUnitOfWork? uow, CancellationToken ct);

    // ---- Service Catalogue: role-aware reads ----

    Task<IReadOnlyList<DepartmentForRole>> ListDepartmentsForRoleAsync(string roleKey, CancellationToken ct);
    Task<IReadOnlyList<SubDepartmentForRole>> ListSubDepartmentsForRoleAsync(Guid departmentId, string roleKey, CancellationToken ct);
    Task<IReadOnlyList<ServiceForRole>> ListServicesByDepartmentForRoleAsync(Guid departmentId, string roleKey, CancellationToken ct);
    Task<ServiceDetailForRole?> GetServiceForRoleAsync(string departmentCode, string serviceCode, string roleKey, CancellationToken ct);

    /// <summary>Every citizen-visible, active service across every department in one call — backs the AI backend's catalog ingestion export, not the /catalog page (which reads department-at-a-time).</summary>
    Task<IReadOnlyList<ServiceForAiExport>> ListServicesForAiExportAsync(CancellationToken ct);

    Task<IReadOnlyList<DepartmentForRole>> SearchDepartmentsAsync(string query, string roleKey, CancellationToken ct);
    Task<IReadOnlyList<ServiceSearchResultRow>> SearchServicesForRoleAsync(string query, string roleKey, CancellationToken ct);

    // ---- Apply demand tracking ----

    /// <summary>Logs one apply attempt (real token mint or homepage-fallback) -- never called for testMode mints. See ServiceApplyTrack.</summary>
    Task InsertServiceApplyTrackAsync(ServiceApplyTrack track, CancellationToken ct);

    Task<long> GetServiceApplyCountAsync(Guid schemeId, CancellationToken ct);
}

public sealed class ServiceSearchResultRow
{
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string ServiceId { get; set; } = "";
    public string? SubDepartmentName { get; set; }
    public string Name { get; set; } = "";
    public string? NameKannada { get; set; }
    public string? ShortDescription { get; set; }
    public int SakalaDays { get; set; }
}

/// <summary>All the fields the catalog ingestion pipeline actually owns for an upsert. Deliberately excludes Fee (legacy free-text, superseded by DepartmentFee/ServiceCharge — see V23's comment), AliasesJson (curated separately, not part of the source spreadsheet), and Portal (legacy free-text) — sp_upsert_sevaone_service leaves all three untouched on conflict. DocumentsJson/DocumentsJsonKannada ARE included: a checklist-style "Required Documents" list has no URL at all, so it belongs here rather than in sevaone_service_document (NOT NULL document_url, reserved for sources that provide one).</summary>
public sealed class ServiceUpsertRequest
{
    public Guid Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid? SubDepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameKannada { get; set; }
    public string? Category { get; set; }
    public int? SakalaDays { get; set; }
    public bool? IsSakalaEnabled { get; set; }
    public string? SakalaLabelRaw { get; set; }
    public string? Description { get; set; }
    public string? DescriptionKannada { get; set; }
    public string? ShortDescription { get; set; }
    public decimal? DepartmentFee { get; set; }
    public decimal? ServiceCharge { get; set; }
    public string? DocumentsJson { get; set; }
    public string? DocumentsJsonKannada { get; set; }
    public string? EligibilityJson { get; set; }
    public string? EligibilityJsonKannada { get; set; }
    public string? StepsJson { get; set; }
    public string? StepsJsonKannada { get; set; }
    public string? Jurisdiction { get; set; }

    /// <summary>Must already be normalized to 'Online'/'Offline'/'Both' — the DB CHECK constraint rejects anything else.</summary>
    public string? ModeOfService { get; set; }

    public string? DepartmentServiceId { get; set; }
    public string? ServiceUrl { get; set; }
    public string? DeeplinkCitizenUrl { get; set; }
    public string? DeeplinkCitizenUrlKannada { get; set; }
}

/// <summary>Plain settable-property class, not a positional record — see SchemeWithDepartment's note on Dapper's materializer.</summary>
public sealed class DepartmentForRole
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? DepartmentUrl { get; set; }
    public string? DepartmentUrlKannada { get; set; }
    public int SubDepartmentCount { get; set; }

    /// <summary>Only services actually visible to the requested role — never the department's true total.</summary>
    public int ServiceCount { get; set; }
}

public sealed class SubDepartmentForRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int DisplayOrder { get; set; }
    public int ServiceCount { get; set; }
}

public class ServiceForRole
{
    public Guid Id { get; set; }
    public string ServiceId { get; set; } = "";
    public Guid? SubDepartmentId { get; set; }
    public string? SubDepartmentName { get; set; }
    public string Name { get; set; } = "";
    public string? NameKannada { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? DescriptionKannada { get; set; }
    public bool? IsSakalaEnabled { get; set; }
    public string? SakalaLabelRaw { get; set; }
    public int SakalaDays { get; set; }
    public decimal? DepartmentFee { get; set; }
    public decimal? ServiceCharge { get; set; }
    public string DocumentsJson { get; set; } = "[]";
    public string DocumentsJsonKannada { get; set; } = "[]";
    public string EligibilityJson { get; set; } = "[]";
    public string EligibilityJsonKannada { get; set; } = "[]";
    public string StepsJson { get; set; } = "[]";
    public string StepsJsonKannada { get; set; } = "[]";
    public string? Jurisdiction { get; set; }
    public string? Category { get; set; }
    public string? ModeOfService { get; set; }
    public string? ApplyServiceUrl { get; set; }
    public string? ApplyServiceUrlKannada { get; set; }
    public string? ServiceUrl { get; set; }
    public string? IntegrationMethod { get; set; }
}

/// <summary>ServiceForRole plus the owning department's code/name — sp_get_service_for_role looks up by (department code, service code), so the caller already needs the department confirmed, not just the service.</summary>
public sealed class ServiceDetailForRole : ServiceForRole
{
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
}

/// <summary>ServiceForRole plus department identity and Aliases — the AI catalog export
/// (sp_list_services_for_ai_export) spans every department in one call, unlike the
/// by-department reads above, so each row needs to carry its own department context.</summary>
public sealed class ServiceForAiExport : ServiceForRole
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string AliasesJson { get; set; } = "[]";
}

/// <summary>Plain settable-property class, not a positional record — Dapper's default materializer binds these reliably via property setters; constructor-matching for records is comparatively brittle across Dapper versions once a custom ITypeMap is registered (see DapperBootstrap).</summary>
public sealed class SchemeWithDepartment
{
    public string ServiceCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string Category { get; set; } = "";
    public int SakalaDays { get; set; }
    public decimal? DepartmentFee { get; set; }
    public decimal? ServiceCharge { get; set; }
    public string Description { get; set; } = "";
    public string RequiredDocumentsJson { get; set; } = "";
    public string EligibilityJson { get; set; } = "";
    public string AliasesJson { get; set; } = "";
    public string StepsJson { get; set; } = "";
    public string? Portal { get; set; }
    public bool DepartmentIntegrated { get; set; }
    /// <summary>Only populated by sp_list_top_requested_services - every other query leaves
    /// these at their default 0, since apply counts only mean something in that one ranking
    /// context. See CatalogDtos.SchemeSummaryDto's own doc comment.</summary>
    public long CatalogueApplyCount { get; set; }
    public long AiApplyCount { get; set; }
    public long TotalApplyCount { get; set; }
}

/// <summary>Result row for FindActiveSchemeByNameAsync - just enough to log an apply_track row
/// (Id/DepartmentRowId) and call CreateApplyTokenAsync (DepartmentCode/ServiceCode), without a
/// second lookup.</summary>
public sealed class ActiveSchemeByName
{
    public Guid Id { get; set; }
    public string ServiceCode { get; set; } = "";
    public Guid DepartmentRowId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class DepartmentWithSchemeCount
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string PortalName { get; set; } = "";
    public string PortalDomain { get; set; } = "";
    public bool IsIntegrated { get; set; }
    public int SchemeCount { get; set; }
}
