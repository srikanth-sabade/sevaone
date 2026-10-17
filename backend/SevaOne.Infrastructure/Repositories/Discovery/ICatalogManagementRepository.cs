using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Discovery;

public interface ICatalogManagementRepository
{
    Task<IReadOnlyList<CatalogAdminDepartmentRow>> ListDepartmentsAsync(CancellationToken ct);
    Task<CatalogAdminDepartmentRow?> GetDepartmentAsync(string code, CancellationToken ct);
    Task<Guid> SaveDepartmentAsync(Guid id, string code, string name, string? nameKannada, string? url, string? urlKannada, int displayOrder, IUnitOfWork uow, CancellationToken ct);
    Task<bool> SetDepartmentActiveAsync(string code, bool isActive, IUnitOfWork uow, CancellationToken ct);
    Task<IReadOnlyList<CatalogAdminSubDepartmentRow>> ListSubDepartmentsAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct);
    Task<CatalogAdminSubDepartmentRow?> GetSubDepartmentAsync(Guid id, CancellationToken ct);
    Task<Guid> SaveSubDepartmentAsync(Guid id, string departmentCode, string name, string? nameKannada, int displayOrder, bool isActive, IUnitOfWork uow, CancellationToken ct);
    Task<bool> SetSubDepartmentActiveAsync(Guid id, bool isActive, IUnitOfWork uow, CancellationToken ct);
    Task<IReadOnlyList<CatalogAdminServiceRow>> ListServicesAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct);
    Task<CatalogAdminServiceRow?> GetServiceAsync(string departmentCode, string serviceCode, CancellationToken ct);
    Task<Guid> SaveServiceAsync(CatalogAdminServiceWriteValues values, IUnitOfWork uow, CancellationToken ct);
    Task<bool> SetServiceActiveAsync(string departmentCode, string serviceCode, bool isActive, IUnitOfWork uow, CancellationToken ct);
    Task<IReadOnlyList<CatalogAdminChangeRow>> ListChangesAsync(int limit, CancellationToken ct);
    Task<Guid> InsertChangeAsync(Guid? serviceId, string changedBy, string changeType, string? diff, IUnitOfWork uow, CancellationToken ct);
}

public sealed class CatalogAdminSubDepartmentRow
{
    public Guid Id { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? NameKannada { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CatalogAdminDepartmentRow
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? DepartmentNameKannada { get; set; }
    public string? DepartmentUrl { get; set; }
    public string? DepartmentUrlKannada { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ServiceCount { get; set; }
}

public class CatalogAdminServiceListRow
{
    public Guid Id { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string ServiceCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? NameKannada { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
}

public sealed class CatalogAdminServiceRow : CatalogAdminServiceListRow
{
    public string? SubDepartmentName { get; set; }
    public string? ShortDescription { get; set; }
    public string? ShortDescriptionKannada { get; set; }
    public string? Description { get; set; }
    public string? DescriptionKannada { get; set; }
    public bool? IsSakalaEnabled { get; set; }
    public string? SakalaLabelRaw { get; set; }
    public int? SakalaDays { get; set; }
    public decimal? DepartmentFee { get; set; }
    public decimal? ServiceCharge { get; set; }
    public string DocumentsJson { get; set; } = "[]";
    public string DocumentsKannadaJson { get; set; } = "[]";
    public string EligibilityJson { get; set; } = "[]";
    public string EligibilityKannadaJson { get; set; } = "[]";
    public string AliasesJson { get; set; } = "[]";
    public string StepsJson { get; set; } = "[]";
    public string StepsKannadaJson { get; set; } = "[]";
    public string? Jurisdiction { get; set; }
    public string? ModeOfService { get; set; }
    public string? DepartmentServiceId { get; set; }
    public string? Portal { get; set; }
    public string? ServiceUrl { get; set; }
    public string? DeeplinkCitizenUrl { get; set; }
    public string? DeeplinkCitizenUrlKannada { get; set; }
    public string? DeeplinkOperatorUrl { get; set; }
    public string? HomepageUrl { get; set; }
    public string? IntegrationMethod { get; set; }
    public string? SevasindhuServiceId { get; set; }
    public string? Remarks { get; set; }
    public string VisibleRolesJson { get; set; } = "[]";
}

public sealed class CatalogAdminChangeRow
{
    public Guid Id { get; set; }
    public string? EntityType { get; set; }
    public string? DepartmentCode { get; set; }
    public string? ServiceCode { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string ChangedBy { get; set; } = "";
    public string ChangeType { get; set; } = "";
    public string? Diff { get; set; }
}

public sealed record CatalogAdminServiceWriteValues(
    Guid Id,
    string DepartmentCode,
    string ServiceCode,
    string Name,
    string? NameKannada,
    string? SubDepartmentName,
    string? ShortDescription,
    string? ShortDescriptionKannada,
    string? Description,
    string? DescriptionKannada,
    bool? IsSakalaEnabled,
    string? SakalaLabelRaw,
    int? SakalaDays,
    decimal? DepartmentFee,
    decimal? ServiceCharge,
    string DocumentsJson,
    string DocumentsKannadaJson,
    string EligibilityJson,
    string EligibilityKannadaJson,
    string AliasesJson,
    string StepsJson,
    string StepsKannadaJson,
    string? Jurisdiction,
    string? Category,
    string? ModeOfService,
    string? DepartmentServiceId,
    string? Portal,
    string? ServiceUrl,
    string? DeeplinkCitizenUrl,
    string? DeeplinkCitizenUrlKannada,
    string? DeeplinkOperatorUrl,
    string? HomepageUrl,
    string? IntegrationMethod,
    string? SevasindhuServiceId,
    string? Remarks,
    bool IsActive);
