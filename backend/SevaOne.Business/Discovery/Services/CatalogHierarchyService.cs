using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Services;

public sealed class CatalogHierarchyService : ICatalogHierarchyService
{
    // Same rationale/TTL as CatalogService's cache -- catalog data changes rarely (a handful
    // of admin publishes a day at most), so a short in-process cache trades a little
    // staleness for cutting DB round trips on the read path every citizen hits browsing
    // /catalog. Cache keys are role-scoped since results differ per role.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IDiscoveryRepository _discovery;
    private readonly IMemoryCache _cache;

    public CatalogHierarchyService(IDiscoveryRepository discovery, IMemoryCache cache)
    {
        _discovery = discovery;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CatalogHierarchyDepartmentDto>> ListDepartmentsAsync(string role, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync($"catalog:hierarchy:departments:{role}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            var rows = await _discovery.ListDepartmentsForRoleAsync(role, ct);
            return (IReadOnlyList<CatalogHierarchyDepartmentDto>)rows
                .Select(d => new CatalogHierarchyDepartmentDto(d.Code, d.Name, d.DepartmentUrl, d.DepartmentUrlKannada, d.SubDepartmentCount, d.ServiceCount))
                .ToList();
        }) ?? [];
    }

    public async Task<CatalogDepartmentDetailDto?> GetDepartmentAsync(string departmentCode, string role, CancellationToken ct)
    {
        var department = await _discovery.GetDepartmentByCodeAsync(departmentCode, ct);
        if (department is null) return null;

        var subDepartments = await _discovery.ListSubDepartmentsForRoleAsync(department.Id, role, ct);
        var services = await _discovery.ListServicesByDepartmentForRoleAsync(department.Id, role, ct);

        return new CatalogDepartmentDetailDto(
            department.Code,
            department.Name,
            department.DepartmentUrl,
            department.DepartmentUrlKannada,
            subDepartments.Select(s => new CatalogHierarchySubDepartmentDto(s.Name, s.ServiceCount)).ToList(),
            services.Select(s => ToServiceDto(departmentCode, s)).ToList());
    }

    public async Task<CatalogHierarchyServiceDto?> GetServiceAsync(string departmentCode, string serviceCode, string role, CancellationToken ct)
    {
        var service = await _discovery.GetServiceForRoleAsync(departmentCode, serviceCode, role, ct);
        return service is null ? null : ToServiceDto(departmentCode, service);
    }

    public async Task<CatalogSearchResultDto> SearchAsync(string query, string role, CancellationToken ct)
    {
        var departments = await _discovery.SearchDepartmentsAsync(query, role, ct);
        var services = await _discovery.SearchServicesForRoleAsync(query, role, ct);

        return new CatalogSearchResultDto(
            departments.Select(d => new CatalogHierarchyDepartmentDto(d.Code, d.Name, d.DepartmentUrl, d.DepartmentUrlKannada, d.SubDepartmentCount, d.ServiceCount)).ToList(),
            services.Select(s => new CatalogSearchServiceDto(
                s.DepartmentCode, s.DepartmentName, s.ServiceId, $"{s.DepartmentCode}-{s.ServiceId}",
                s.SubDepartmentName, s.Name, s.NameKannada, s.ShortDescription, s.SakalaDays)).ToList());
    }

    private static CatalogHierarchyServiceDto ToServiceDto(string departmentCode, ServiceForRole s) => new(
        s.ServiceId,
        $"{departmentCode}-{s.ServiceId}",
        s.SubDepartmentName,
        s.Name,
        s.NameKannada,
        s.ShortDescription,
        s.Description,
        s.DescriptionKannada,
        s.IsSakalaEnabled,
        s.SakalaLabelRaw,
        s.SakalaDays,
        s.DepartmentFee,
        s.ServiceCharge,
        ParseList(s.DocumentsJson),
        ParseList(s.DocumentsJsonKannada),
        ParseList(s.EligibilityJson),
        ParseList(s.EligibilityJsonKannada),
        ParseList(s.StepsJson),
        ParseList(s.StepsJsonKannada),
        s.Jurisdiction,
        s.Category,
        s.ModeOfService,
        s.ApplyServiceUrl,
        s.ApplyServiceUrlKannada,
        s.ServiceUrl,
        s.IntegrationMethod);

    private static List<string> ParseList(string? json) =>
        string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];

    public async Task<IReadOnlyList<AiCatalogExportServiceDto>> ExportForAiAsync(CancellationToken ct)
    {
        var rows = await _discovery.ListServicesForAiExportAsync(ct);
        return rows.Select(ToAiExportDto).ToList();
    }

    private static AiCatalogExportServiceDto ToAiExportDto(ServiceForAiExport s) => new(
        s.DepartmentCode,
        s.DepartmentName,
        s.SubDepartmentName,
        s.ServiceId,
        $"{s.DepartmentCode}-{s.ServiceId}",
        s.Name,
        s.NameKannada,
        s.ShortDescription,
        s.Description,
        s.DescriptionKannada,
        s.SakalaDays,
        s.DepartmentFee,
        s.ServiceCharge,
        ParseList(s.DocumentsJson),
        ParseList(s.DocumentsJsonKannada),
        ParseList(s.EligibilityJson),
        ParseList(s.EligibilityJsonKannada),
        ParseList(s.StepsJson),
        ParseList(s.StepsJsonKannada),
        ParseList(s.AliasesJson),
        s.Jurisdiction,
        s.Category,
        s.ModeOfService,
        s.ApplyServiceUrl,
        s.ApplyServiceUrlKannada,
        s.ServiceUrl);
}
