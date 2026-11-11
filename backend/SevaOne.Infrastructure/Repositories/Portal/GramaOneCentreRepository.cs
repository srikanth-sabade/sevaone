using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Portal;

public sealed class NearestGramaOneCentre
{
    public int Id { get; set; }
    public string District { get; set; } = string.Empty;
    public string Taluk { get; set; } = string.Empty;
    public string GramPanchayat { get; set; } = string.Empty;
    public string Village { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistanceKm { get; set; }
}

public interface IGramaOneCentreRepository
{
    Task<IReadOnlyList<NearestGramaOneCentre>> NearestAsync(double latitude, double longitude, int limit, CancellationToken ct);
    Task<IReadOnlyList<string>> DistrictsAsync(CancellationToken ct);
    Task<IReadOnlyList<NearestGramaOneCentre>> SearchAsync(string district, string? query, int limit, CancellationToken ct);
}

public sealed class GramaOneCentreRepository : RepositoryBase, IGramaOneCentreRepository
{
    private readonly IDbConnectionFactory _factory;

    public GramaOneCentreRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<NearestGramaOneCentre>> NearestAsync(double latitude, double longitude, int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<NearestGramaOneCentre>(new CommandDefinition(
            "SELECT * FROM portal.sp_nearest_gramaone_centres(@Lat, @Lon, @Limit)",
            new { Lat = latitude, Lon = longitude, Limit = limit }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<string>> DistrictsAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<string>(new CommandDefinition("SELECT district FROM portal.sp_list_gramaone_districts()", cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<NearestGramaOneCentre>> SearchAsync(string district, string? query, int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<NearestGramaOneCentre>(new CommandDefinition(
            "SELECT * FROM portal.sp_search_gramaone_centres(@District, @Query, @Limit)",
            new { District = district, Query = query, Limit = limit }, cancellationToken: ct));
        return rows.AsList();
    }
}
