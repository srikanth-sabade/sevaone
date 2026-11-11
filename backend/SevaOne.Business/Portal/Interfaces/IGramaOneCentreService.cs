using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IGramaOneCentreService
{
    Task<IReadOnlyList<GramaOneCentreDto>> NearestAsync(double latitude, double longitude, int limit, CancellationToken ct);
    Task<IReadOnlyList<string>> DistrictsAsync(CancellationToken ct);
    Task<IReadOnlyList<GramaOneCentreDto>> SearchAsync(string district, string? query, CancellationToken ct);
}
