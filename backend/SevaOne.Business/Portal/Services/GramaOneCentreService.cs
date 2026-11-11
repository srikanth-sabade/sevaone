using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.Repositories.Portal;

namespace SevaOne.Business.Portal.Services;

public sealed class GramaOneCentreService : IGramaOneCentreService
{
    private readonly IGramaOneCentreRepository _repo;

    public GramaOneCentreService(IGramaOneCentreRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<GramaOneCentreDto>> NearestAsync(double latitude, double longitude, int limit, CancellationToken ct)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new DomainException("Latitude or longitude is out of range.");
        }

        var rows = await _repo.NearestAsync(latitude, longitude, Math.Clamp(limit, 1, 20), ct);
        return rows.Select(static c => new GramaOneCentreDto(
            c.Id, c.District, c.Taluk, c.GramPanchayat, c.Village, c.Latitude, c.Longitude, c.DistanceKm is { } km ? Math.Round(km, 1) : null)).ToArray();
    }

    public Task<IReadOnlyList<string>> DistrictsAsync(CancellationToken ct) => _repo.DistrictsAsync(ct);

    public async Task<IReadOnlyList<GramaOneCentreDto>> SearchAsync(string district, string? query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(district))
        {
            throw new DomainException("Choose a district.");
        }

        var rows = await _repo.SearchAsync(district.Trim(), query?.Trim(), 60, ct);
        return rows.Select(static c => new GramaOneCentreDto(
            c.Id, c.District, c.Taluk, c.GramPanchayat, c.Village, c.Latitude, c.Longitude, null)).ToArray();
    }
}
