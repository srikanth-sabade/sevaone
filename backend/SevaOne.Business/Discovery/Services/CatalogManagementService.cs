using System.Text.Json;
using System.Text.RegularExpressions;
using SevaOne.Business.Common;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Infrastructure.Repositories.Common;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Services;

public sealed class CatalogManagementService : ICatalogManagementService
{
    private static readonly Regex CodePattern = new("^[A-Za-z0-9][A-Za-z0-9_-]{1,63}$", RegexOptions.Compiled);
    private static readonly Regex ServiceCodePattern = new("^[A-Za-z0-9][A-Za-z0-9_-]{1,31}$", RegexOptions.Compiled);
    private readonly ICatalogManagementRepository _catalog;
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly ICurrentGovAdmin _current;

    public CatalogManagementService(ICatalogManagementRepository catalog, IUnitOfWorkFactory uowFactory, ICurrentGovAdmin current)
    {
        _catalog = catalog;
        _uowFactory = uowFactory;
        _current = current;
    }

    public async Task<IReadOnlyList<CatalogAdminDepartmentDto>> ListDepartmentsAsync(CancellationToken ct) =>
        (await _catalog.ListDepartmentsAsync(ct)).Select(ToDepartment).ToList();

    public async Task<CatalogAdminDepartmentDto> CreateDepartmentAsync(CatalogAdminDepartmentWriteRequest request, CancellationToken ct)
    {
        RequireSuperadmin();
        ValidateDepartment(request);
        var code = request.Code.Trim();
        if (await _catalog.GetDepartmentAsync(code, ct) is not null)
            throw new ConflictException($"Department '{code}' already exists.");

        await using var uow = await _uowFactory.BeginAsync(ct);
        var id = await _catalog.SaveDepartmentAsync(Guid.NewGuid(), code, request.Name.Trim(), Clean(request.DepartmentNameKannada), Clean(request.DepartmentUrl), Clean(request.DepartmentUrlKannada), request.DisplayOrder, uow, ct);
        await _catalog.InsertChangeAsync(null, Actor(), "created", JsonSerializer.Serialize(new { entityType = "department", departmentCode = code, oldValue = (object?)null, newValue = request }), uow, ct);
        await uow.CommitAsync(ct);
        return await RequireDepartment(code, ct);
    }

    public async Task<CatalogAdminDepartmentDto> UpdateDepartmentAsync(string code, CatalogAdminDepartmentWriteRequest request, CancellationToken ct)
    {
        RequireSuperadmin();
        ValidateDepartment(request);
        var existing = await RequireDepartmentRow(code, ct);
        if (!string.Equals(code.Trim(), request.Code.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Department code cannot be changed after creation.");

        await using var uow = await _uowFactory.BeginAsync(ct);
        await _catalog.SaveDepartmentAsync(existing.Id, existing.Code, request.Name.Trim(), Clean(request.DepartmentNameKannada), Clean(request.DepartmentUrl), Clean(request.DepartmentUrlKannada), request.DisplayOrder, uow, ct);
        await _catalog.InsertChangeAsync(null, Actor(), "updated", JsonSerializer.Serialize(new { entityType = "department", departmentCode = existing.Code, oldValue = existing, newValue = request }), uow, ct);
        await uow.CommitAsync(ct);
        return await RequireDepartment(existing.Code, ct);
    }

    public async Task SetDepartmentActiveAsync(string code, bool isActive, CancellationToken ct)
    {
        RequireSuperadmin();
        var existing = await RequireDepartmentRow(code, ct);
        if (existing.IsActive == isActive) return;
        await using var uow = await _uowFactory.BeginAsync(ct);
        if (!await _catalog.SetDepartmentActiveAsync(existing.Code, isActive, uow, ct))
            throw new NotFoundException($"Department '{code}' was not found.");
        await _catalog.InsertChangeAsync(null, Actor(), isActive ? "reactivated" : "deactivated", JsonSerializer.Serialize(new { entityType = "department", departmentCode = existing.Code, oldValue = new { isActive = existing.IsActive }, newValue = new { isActive } }), uow, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogAdminSubDepartmentDto>> ListSubDepartmentsAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct) =>
        (await _catalog.ListSubDepartmentsAsync(Clean(departmentCode), Clean(search), isActive, ct)).Select(ToSubDepartment).ToList();

    public async Task<CatalogAdminSubDepartmentDto> CreateSubDepartmentAsync(CatalogAdminSubDepartmentWriteRequest request, CancellationToken ct)
    {
        RequireSuperadmin();
        ValidateSubDepartment(request);
        var departmentCode = request.DepartmentCode.Trim();
        var duplicate = (await _catalog.ListSubDepartmentsAsync(departmentCode, null, null, ct))
            .Any(row => string.Equals(row.Name.Trim(), request.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicate) throw new ConflictException($"Sub-department '{request.Name.Trim()}' already exists in '{departmentCode}'.");

        await using var uow = await _uowFactory.BeginAsync(ct);
        var id = await _catalog.SaveSubDepartmentAsync(Guid.NewGuid(), departmentCode, request.Name.Trim(), Clean(request.NameKannada), request.DisplayOrder, request.IsActive, uow, ct);
        await _catalog.InsertChangeAsync(null, Actor(), "sub_department_created", JsonSerializer.Serialize(new { entityType = "sub_department", departmentCode, newValue = request }), uow, ct);
        await uow.CommitAsync(ct);
        return await RequireSubDepartment(id, ct);
    }

    public async Task<CatalogAdminSubDepartmentDto> UpdateSubDepartmentAsync(Guid id, CatalogAdminSubDepartmentWriteRequest request, CancellationToken ct)
    {
        RequireSuperadmin();
        ValidateSubDepartment(request);
        var existing = await RequireSubDepartmentRow(id, ct);
        if (!string.Equals(existing.DepartmentCode, request.DepartmentCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("A sub-department cannot be moved to another department.");
        var duplicate = (await _catalog.ListSubDepartmentsAsync(existing.DepartmentCode, null, null, ct))
            .Any(row => row.Id != id && string.Equals(row.Name.Trim(), request.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicate) throw new ConflictException($"Sub-department '{request.Name.Trim()}' already exists in '{existing.DepartmentCode}'.");

        await using var uow = await _uowFactory.BeginAsync(ct);
        await _catalog.SaveSubDepartmentAsync(id, existing.DepartmentCode, request.Name.Trim(), Clean(request.NameKannada), request.DisplayOrder, request.IsActive, uow, ct);
        await _catalog.InsertChangeAsync(null, Actor(), "sub_department_updated", JsonSerializer.Serialize(new { entityType = "sub_department", departmentCode = existing.DepartmentCode, oldValue = existing, newValue = request }), uow, ct);
        await uow.CommitAsync(ct);
        return await RequireSubDepartment(id, ct);
    }

    public async Task SetSubDepartmentActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        RequireSuperadmin();
        var existing = await RequireSubDepartmentRow(id, ct);
        if (existing.IsActive == isActive) return;
        await using var uow = await _uowFactory.BeginAsync(ct);
        var found = await _catalog.SetSubDepartmentActiveAsync(id, isActive, uow, ct);
        if (!found) throw new NotFoundException($"Sub-department '{id}' was not found.");
        await _catalog.InsertChangeAsync(null, Actor(), isActive ? "sub_department_reactivated" : "sub_department_deactivated", JsonSerializer.Serialize(new { entityType = "sub_department", departmentCode = existing.DepartmentCode, subDepartmentId = id, isActive }), uow, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogAdminServiceListItemDto>> ListServicesAsync(string? departmentCode, string? search, bool? isActive, CancellationToken ct) =>
        (await _catalog.ListServicesAsync(Clean(departmentCode), Clean(search), isActive, ct)).Select(ToServiceListItem).ToList();

    public async Task<CatalogAdminServiceDto> GetServiceAsync(string departmentCode, string serviceCode, CancellationToken ct) =>
        ToService(await RequireServiceRow(departmentCode, serviceCode, ct));

    public async Task<CatalogAdminServiceDto> CreateServiceAsync(CatalogAdminServiceWriteRequest request, CancellationToken ct)
    {
        RequireSuperadmin();
        ValidateService(request);
        if (await _catalog.GetServiceAsync(request.DepartmentCode, request.ServiceCode, ct) is not null)
            throw new ConflictException($"Service '{request.ServiceCode}' already exists under '{request.DepartmentCode}'.");
        return await SaveService(request, Guid.NewGuid(), "created", null, ct);
    }

    public async Task<CatalogAdminServiceDto> UpdateServiceAsync(string departmentCode, string serviceCode, CatalogAdminServiceWriteRequest request, CancellationToken ct)
    {
        RequireSuperadmin();
        ValidateService(request);
        if (!string.Equals(departmentCode.Trim(), request.DepartmentCode.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(serviceCode.Trim(), request.ServiceCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Department and service codes cannot be changed after creation.");
        var existing = await RequireServiceRow(departmentCode, serviceCode, ct);
        return await SaveService(request, existing.Id, "updated", existing, ct);
    }

    public async Task SetServiceActiveAsync(string departmentCode, string serviceCode, bool isActive, CancellationToken ct)
    {
        RequireSuperadmin();
        var existing = await RequireServiceRow(departmentCode, serviceCode, ct);
        if (existing.IsActive == isActive) return;
        await using var uow = await _uowFactory.BeginAsync(ct);
        if (!await _catalog.SetServiceActiveAsync(existing.DepartmentCode, existing.ServiceCode, isActive, uow, ct))
            throw new NotFoundException($"Service '{serviceCode}' was not found under '{departmentCode}'.");
        await _catalog.InsertChangeAsync(existing.Id, Actor(), isActive ? "reactivated" : "deactivated", JsonSerializer.Serialize(new { entityType = "service", departmentCode = existing.DepartmentCode, serviceCode = existing.ServiceCode, oldValue = new { isActive = existing.IsActive }, newValue = new { isActive } }), uow, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogAdminChangeDto>> ListChangesAsync(int limit, CancellationToken ct)
    {
        RequireSuperadmin();
        var safeLimit = Math.Clamp(limit, 1, 200);
        return (await _catalog.ListChangesAsync(safeLimit, ct)).Select(r => new CatalogAdminChangeDto(
            r.Id, r.EntityType ?? "service", r.DepartmentCode, r.ServiceCode, r.ChangedAt,
            r.ChangedBy, r.ChangeType, r.Diff)).ToList();
    }

    private async Task<CatalogAdminServiceDto> SaveService(CatalogAdminServiceWriteRequest request, Guid id, string changeType, CatalogAdminServiceRow? old, CancellationToken ct)
    {
        var values = new CatalogAdminServiceWriteValues(
            id, request.DepartmentCode.Trim(), request.ServiceCode.Trim(), request.Name.Trim(), Clean(request.NameKannada),
            Clean(request.SubDepartmentName), Clean(request.ShortDescription), Clean(request.ShortDescriptionKannada),
            Clean(request.Description), Clean(request.DescriptionKannada), request.IsSakalaEnabled, Clean(request.SakalaLabelRaw),
            request.SakalaDays, request.DepartmentFee, request.ServiceCharge, JsonList(request.Documents), JsonList(request.DocumentsKannada),
            JsonList(request.Eligibility), JsonList(request.EligibilityKannada), JsonList(request.Aliases), JsonList(request.Steps),
            JsonList(request.StepsKannada), Clean(request.Jurisdiction), Clean(request.Category), Clean(request.ModeOfService),
            Clean(request.DepartmentServiceId), Clean(request.Portal), Clean(request.ServiceUrl), Clean(request.DeeplinkCitizenUrl),
            Clean(request.DeeplinkCitizenUrlKannada), Clean(request.DeeplinkOperatorUrl), Clean(request.HomepageUrl),
            Clean(request.IntegrationMethod), Clean(request.SevasindhuServiceId), Clean(request.Remarks), request.IsActive);

        await using var uow = await _uowFactory.BeginAsync(ct);
        var serviceId = await _catalog.SaveServiceAsync(values, uow, ct);
        await _catalog.InsertChangeAsync(serviceId, Actor(), changeType, JsonSerializer.Serialize(new
        {
            entityType = "service", departmentCode = values.DepartmentCode, serviceCode = values.ServiceCode,
            oldValue = old is null ? null : new { old.Name, old.NameKannada, old.IsActive, old.Version },
            newValue = new { values.Name, values.NameKannada, values.IsActive }
        }), uow, ct);
        await uow.CommitAsync(ct);
        return await GetServiceAsync(values.DepartmentCode, values.ServiceCode, ct);
    }

    private async Task<CatalogAdminDepartmentRow> RequireDepartmentRow(string code, CancellationToken ct) =>
        await _catalog.GetDepartmentAsync(code.Trim(), ct) ?? throw new NotFoundException($"Department '{code}' was not found.");

    private async Task<CatalogAdminDepartmentDto> RequireDepartment(string code, CancellationToken ct) => ToDepartment(await RequireDepartmentRow(code, ct));

    private async Task<CatalogAdminSubDepartmentRow> RequireSubDepartmentRow(Guid id, CancellationToken ct) =>
        await _catalog.GetSubDepartmentAsync(id, ct) ?? throw new NotFoundException($"Sub-department '{id}' was not found.");

    private async Task<CatalogAdminSubDepartmentDto> RequireSubDepartment(Guid id, CancellationToken ct) => ToSubDepartment(await RequireSubDepartmentRow(id, ct));

    private async Task<CatalogAdminServiceRow> RequireServiceRow(string departmentCode, string serviceCode, CancellationToken ct) =>
        await _catalog.GetServiceAsync(departmentCode.Trim(), serviceCode.Trim(), ct)
        ?? throw new NotFoundException($"Service '{serviceCode}' was not found under '{departmentCode}'.");

    private string Actor() => _current.DisplayName ?? _current.Wso2Subject ?? "unknown-gov-admin";

    private void RequireSuperadmin()
    {
        if (_current.Tier != GovAdminTier.Superadmin)
            throw new ForbiddenException("Only a Superadmin can manage the service catalogue.");
    }

    private static void ValidateDepartment(CatalogAdminDepartmentWriteRequest request)
    {
        if (!CodePattern.IsMatch(request.Code.Trim())) throw new DomainException("Use 2-64 letters, numbers, hyphens, or underscores for the department code.");
        if (request.Name.Trim().Length is < 2 or > 200) throw new DomainException("Department name must be between 2 and 200 characters.");
        if (request.DisplayOrder < 0) throw new DomainException("Display order cannot be negative.");
        ValidateUrl(request.DepartmentUrl, "Department URL"); ValidateUrl(request.DepartmentUrlKannada, "Kannada department URL");
    }

    private static void ValidateService(CatalogAdminServiceWriteRequest request)
    {
        if (!ServiceCodePattern.IsMatch(request.ServiceCode.Trim())) throw new DomainException("Use 2-32 letters, numbers, hyphens, or underscores for the service code.");
        if (string.IsNullOrWhiteSpace(request.DepartmentCode)) throw new DomainException("Department code is required.");
        if (request.Name.Trim().Length is < 2 or > 300) throw new DomainException("Service name must be between 2 and 300 characters.");
        if (request.SakalaDays is < 0) throw new DomainException("Sakala days cannot be negative.");
        if (request.DepartmentFee is < 0 || request.ServiceCharge is < 0) throw new DomainException("Fees cannot be negative.");
        if (request.ModeOfService is not null && request.ModeOfService is not ("Online" or "Offline" or "Both")) throw new DomainException("Mode of service must be Online, Offline, or Both.");
        if (request.IntegrationMethod is not null && request.IntegrationMethod is not ("ajsk" or "sakala_token")) throw new DomainException("Choose a supported integration method.");
        ValidateUrl(request.ServiceUrl, "Service URL"); ValidateUrl(request.DeeplinkCitizenUrl, "Citizen apply URL");
        ValidateUrl(request.DeeplinkCitizenUrlKannada, "Kannada citizen apply URL"); ValidateUrl(request.DeeplinkOperatorUrl, "Operator apply URL"); ValidateUrl(request.HomepageUrl, "Homepage URL");
    }

    private static void ValidateSubDepartment(CatalogAdminSubDepartmentWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentCode)) throw new DomainException("Department code is required.");
        if (request.Name.Trim().Length is < 2 or > 200) throw new DomainException("Sub-department name must be between 2 and 200 characters.");
        if (request.NameKannada?.Trim().Length > 200) throw new DomainException("Kannada sub-department name cannot exceed 200 characters.");
        if (request.DisplayOrder < 0) throw new DomainException("Display order cannot be negative.");
    }

    private static void ValidateUrl(string? value, string label)
    {
        if (!string.IsNullOrWhiteSpace(value) && (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
            throw new DomainException($"{label} must be a valid http or https URL.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string JsonList(IReadOnlyList<string>? values) => JsonSerializer.Serialize((values ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList());
    private static CatalogAdminDepartmentDto ToDepartment(CatalogAdminDepartmentRow r) => new(r.Id, r.Code, r.Name, r.DepartmentNameKannada, r.DepartmentUrl, r.DepartmentUrlKannada, r.DisplayOrder, r.IsActive, r.ServiceCount);
    private static CatalogAdminSubDepartmentDto ToSubDepartment(CatalogAdminSubDepartmentRow r) => new(r.Id, r.DepartmentCode, r.DepartmentName, r.Name, r.NameKannada, r.DisplayOrder, r.IsActive);
    private static CatalogAdminServiceListItemDto ToServiceListItem(CatalogAdminServiceRow r) => new(r.Id, r.DepartmentCode, r.DepartmentName, r.ServiceCode, r.Name, r.NameKannada, r.Category, r.IsActive, r.Version);

    private static CatalogAdminServiceDto ToService(CatalogAdminServiceRow r) => new(
        r.Id, r.DepartmentCode, r.DepartmentName, r.ServiceCode, r.Name, r.NameKannada, r.SubDepartmentName,
        r.ShortDescription, r.ShortDescriptionKannada, r.Description, r.DescriptionKannada, r.IsSakalaEnabled,
        r.SakalaLabelRaw, r.SakalaDays, r.DepartmentFee, r.ServiceCharge, ParseList(r.DocumentsJson),
        ParseList(r.DocumentsKannadaJson), ParseList(r.EligibilityJson), ParseList(r.EligibilityKannadaJson),
        ParseList(r.AliasesJson), ParseList(r.StepsJson), ParseList(r.StepsKannadaJson), r.Jurisdiction, r.Category,
        r.ModeOfService, r.DepartmentServiceId, r.Portal, r.ServiceUrl, r.DeeplinkCitizenUrl,
        r.DeeplinkCitizenUrlKannada, r.DeeplinkOperatorUrl, r.HomepageUrl, r.IntegrationMethod,
        r.SevasindhuServiceId, r.Remarks, r.IsActive, r.Version, ParseList(r.VisibleRolesJson));

    private static IReadOnlyList<string> ParseList(string? json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? []; }
        catch (JsonException) { return []; }
    }
}
