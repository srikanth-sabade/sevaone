using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Discovery;

public sealed class CatalogVisibilityRepository : RepositoryBase, ICatalogVisibilityRepository
{
    private readonly IDbConnectionFactory _factory;

    public CatalogVisibilityRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<CatalogRole>> ListCatalogRolesAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_list_catalog_roles()", cancellationToken: ct);
        var rows = await conn.QueryAsync<CatalogRole>(cmd);
        return rows.AsList();
    }

    public async Task SetServiceVisibilityAsync(Guid serviceId, Guid roleId, bool grant, string grantedBy, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = "SELECT * FROM discovery.sp_set_service_visibility(@serviceId, @roleId, @grant, @grantedBy)";
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, new { serviceId, roleId, grant, grantedBy }, tx, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<ServiceVisibilityRow>> ListServiceVisibilityAsync(Guid serviceId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_service_visibility(@serviceId)",
            new { serviceId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ServiceVisibilityRow>(cmd);
        return rows.AsList();
    }

    public async Task<Guid> InsertServiceChangeLogAsync(ServiceChangeLog entry, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM discovery.sp_insert_service_change_log(
                @Id, @ServiceId, @BatchId, @ChangedBy, @ChangeType, @Diff)
            """;
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, entry, tx, cancellationToken: ct);
            return await conn.QuerySingleAsync<Guid>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<ServiceChangeLog>> ListServiceChangeLogAsync(Guid? serviceId, int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_service_change_log(@serviceId, @limit)",
            new { serviceId, limit }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ServiceChangeLog>(cmd);
        return rows.AsList();
    }
}
