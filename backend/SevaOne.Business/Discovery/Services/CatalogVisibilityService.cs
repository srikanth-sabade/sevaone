using System.Text.Json;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Services;

public sealed class CatalogVisibilityService : ICatalogVisibilityService
{
    private readonly IDiscoveryRepository _discovery;
    private readonly ICatalogVisibilityRepository _visibility;

    public CatalogVisibilityService(IDiscoveryRepository discovery, ICatalogVisibilityRepository visibility)
    {
        _discovery = discovery;
        _visibility = visibility;
    }

    public async Task<IReadOnlyList<CatalogRoleDto>> ListRolesAsync(CancellationToken ct)
    {
        var roles = await _visibility.ListCatalogRolesAsync(ct);
        return roles.Select(r => new CatalogRoleDto(r.RoleKey, r.DisplayName, r.Description)).ToList();
    }

    public async Task<IReadOnlyList<ServiceVisibilityDto>> GetServiceVisibilityAsync(string departmentCode, string serviceCode, CancellationToken ct)
    {
        var service = await ResolveServiceAsync(departmentCode, serviceCode, ct);
        var rows = await _visibility.ListServiceVisibilityAsync(service.Id, ct);
        return rows.Select(v => new ServiceVisibilityDto(v.RoleKey, v.DisplayName, v.GrantedAt, v.GrantedBy)).ToList();
    }

    public async Task SetServiceVisibilityAsync(string departmentCode, string serviceCode, string roleKey, bool grant, string changedBy, CancellationToken ct)
    {
        var service = await ResolveServiceAsync(departmentCode, serviceCode, ct);
        var role = await ResolveRoleAsync(roleKey, ct);

        await _visibility.SetServiceVisibilityAsync(service.Id, role.Id, grant, changedBy, uow: null, ct);
        await _visibility.InsertServiceChangeLogAsync(new ServiceChangeLog
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            ChangedBy = changedBy,
            ChangeType = grant ? "role_grant" : "role_revoke",
            Diff = JsonSerializer.Serialize(new { role = roleKey }),
        }, uow: null, ct);
    }

    public async Task<BulkVisibilityResult> BulkSetVisibilityAsync(BulkVisibilityRequest request, string changedBy, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            throw new DomainException("DepartmentCode is required for a bulk visibility change -- there's no 'every service in the catalogue' shortcut.");
        }

        var department = await _discovery.GetDepartmentByCodeAsync(request.DepartmentCode, ct)
            ?? throw new NotFoundException($"Department '{request.DepartmentCode}' was not found.");

        var services = await _discovery.ListSchemesAsync(department.Id, ct);

        if (!string.IsNullOrWhiteSpace(request.SubDepartmentName))
        {
            var subDepartments = await _discovery.ListSubDepartmentsByDepartmentAsync(department.Id, ct);
            var subDepartment = subDepartments.FirstOrDefault(s => string.Equals(s.Name, request.SubDepartmentName, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotFoundException($"Sub-department '{request.SubDepartmentName}' was not found under '{request.DepartmentCode}'.");
            services = services.Where(s => s.SubDepartmentId == subDepartment.Id).ToList();
        }

        var roles = await _visibility.ListCatalogRolesAsync(ct);
        var roleIds = request.RoleKeys
            .Select(key => roles.FirstOrDefault(r => r.RoleKey == key)?.Id ?? throw new NotFoundException($"Role '{key}' was not found."))
            .ToList();

        var applied = 0;
        foreach (var service in services)
        {
            foreach (var roleId in roleIds)
            {
                await _visibility.SetServiceVisibilityAsync(service.Id, roleId, request.Grant, changedBy, uow: null, ct);
                applied++;
            }

            await _visibility.InsertServiceChangeLogAsync(new ServiceChangeLog
            {
                Id = Guid.NewGuid(),
                ServiceId = service.Id,
                ChangedBy = changedBy,
                ChangeType = request.Grant ? "role_grant" : "role_revoke",
                Diff = JsonSerializer.Serialize(new { roles = request.RoleKeys, bulk = true }),
            }, uow: null, ct);
        }

        return new BulkVisibilityResult(services.Count, applied);
    }

    public async Task<IReadOnlyList<ServiceChangeLogDto>> ListChangeLogAsync(string? departmentCode, string? serviceCode, int limit, CancellationToken ct)
    {
        Guid? serviceId = null;
        if (!string.IsNullOrWhiteSpace(departmentCode) && !string.IsNullOrWhiteSpace(serviceCode))
        {
            var service = await ResolveServiceAsync(departmentCode, serviceCode, ct);
            serviceId = service.Id;
        }

        var entries = await _visibility.ListServiceChangeLogAsync(serviceId, limit, ct);
        var result = new List<ServiceChangeLogDto>(entries.Count);
        foreach (var entry in entries)
        {
            string? entryServiceCode = null, entryServiceName = null;
            if (entry.ServiceId is { } id)
            {
                var scheme = await _discovery.GetSchemeByIdAsync(id, ct);
                entryServiceCode = scheme?.ServiceCode;
                entryServiceName = scheme?.Name;
            }

            result.Add(new ServiceChangeLogDto(entry.Id, entryServiceCode, entryServiceName, entry.BatchId, entry.ChangedAt, entry.ChangedBy, entry.ChangeType, entry.Diff));
        }

        return result;
    }

    private async Task<Scheme> ResolveServiceAsync(string departmentCode, string serviceCode, CancellationToken ct)
    {
        var department = await _discovery.GetDepartmentByCodeAsync(departmentCode, ct)
            ?? throw new NotFoundException($"Department '{departmentCode}' was not found.");
        return await _discovery.GetSchemeByCodeAsync(department.Id, serviceCode, ct)
            ?? throw new NotFoundException($"Service '{serviceCode}' was not found under '{departmentCode}'.");
    }

    private async Task<CatalogRole> ResolveRoleAsync(string roleKey, CancellationToken ct)
    {
        var roles = await _visibility.ListCatalogRolesAsync(ct);
        return roles.FirstOrDefault(r => r.RoleKey == roleKey) ?? throw new NotFoundException($"Role '{roleKey}' was not found.");
    }
}
