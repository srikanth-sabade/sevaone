using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using SevaOne.Business.Common;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Services;

public sealed class CatalogService : ICatalogService
{
    // Catalog data (departments/schemes) changes rarely -- a handful of admin edits a day at
    // most -- so a short in-process cache trades a little staleness for cutting DB round trips
    // on the read path every citizen hits browsing the catalog.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IDiscoveryRepository _discovery;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public CatalogService(IDiscoveryRepository discovery, IMapper mapper, IMemoryCache cache)
    {
        _discovery = discovery;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync("catalog:departments", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            var rows = await _discovery.ListDepartmentsWithSchemeCountAsync(ct);
            return (IReadOnlyList<DepartmentDto>)_mapper.Map<List<DepartmentDto>>(rows);
        }) ?? [];
    }

    public async Task<IReadOnlyList<SchemeSummaryDto>> ListSchemesAsync(ListSchemesRequest request, CancellationToken ct)
    {
        var cacheKey = $"catalog:schemes:{request.DepartmentCode}:{request.Category}";
        var rows = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _discovery.ListSchemesWithDepartmentAsync(request.DepartmentCode, request.Category, onlyActive: false, ct);
        });
        IEnumerable<SchemeSummaryDto> mapped = _mapper.Map<List<SchemeSummaryDto>>(rows);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.Trim();
            mapped = mapped.Where(s =>
                s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.ServiceCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Aliases.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        return mapped.ToList();
    }

    public async Task<IReadOnlyList<SchemeSummaryDto>> ListPopularSchemesAsync(int limit, CancellationToken ct)
    {
        // A rolling 30-day window, not a strict calendar month -- avoids a hard reset to
        // all-zero counts on the 1st of every month while still matching "this month" in
        // spirit for the landing page's label.
        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var cacheKey = $"catalog:popular:{limit}";
        var rows = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _discovery.ListTopRequestedServicesAsync(since, limit, ct);
        });
        return _mapper.Map<List<SchemeSummaryDto>>(rows);
    }

    public async Task<SchemeDetailDto> GetSchemeAsync(GetSchemeRequest request, CancellationToken ct)
    {
        var cacheKey = $"catalog:scheme:{request.ServiceCode}";
        var s = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _discovery.GetSchemeWithDepartmentByServiceCodeAsync(request.ServiceCode, ct);
        }) ?? throw new NotFoundException($"Scheme {request.ServiceCode} was not found.");

        return _mapper.Map<SchemeDetailDto>(s);
    }

    public async Task<ServiceSearchResultDto> SearchServicesAsync(SearchServicesRequest request, CancellationToken ct)
    {
        var q = request.Q.Trim();
        var candidates = await _discovery.ListSchemesWithDepartmentAsync(null, null, onlyActive: true, ct);

        var matches = candidates.Where(s =>
            s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.ServiceCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            JsonLists.Read(s.AliasesJson).Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matches.Count == 0)
        {
            return new ServiceSearchResultDto("none", null, null);
        }

        if (matches.Count == 1)
        {
            return new ServiceSearchResultDto("single", _mapper.Map<SchemeSummaryDto>(matches[0]), null);
        }

        var options = matches
            .GroupBy(s => (s.Category, s.DepartmentName))
            .Select(g => new ClarifyOptionDto(g.Key.Category, g.Key.DepartmentName, g.Count()))
            .OrderByDescending(o => o.MatchCount)
            .ToList();

        var question = options.Count <= 4
            ? $"I found matches in {options.Count} categories: {string.Join(", ", options.Select(o => $"{o.Category} ({o.DepartmentName})"))}. Which one do you mean?"
            : $"I found matches across {options.Count} categories — could you narrow it down, e.g. by department or what you're trying to do?";

        return new ServiceSearchResultDto("clarify", null, new ClarifyDto(question, options));
    }
}
