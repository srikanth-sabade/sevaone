using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.GovAdmin;

public sealed class GovAdminActivityLogRepository : RepositoryBase, IGovAdminActivityLogRepository
{
    private readonly IDbConnectionFactory _factory;

    public GovAdminActivityLogRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task InsertAsync(GovAdminActivityLog log, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM portal.sp_insert_gov_admin_activity(
                @Id, @ActorUsername, @ActorDisplayName, @Action, @Target, @Detail)
            """;
        using var conn = _factory.CreateConnection();
        await conn.QuerySingleAsync<WriteResult>(new CommandDefinition(sql, log, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<GovAdminActivityLog>> ListAsync(int limit, string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_list_gov_admin_activity(@limit, @action, @from, @to)",
            new { limit, action, from, to }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovAdminActivityLog>(cmd);
        return rows.AsList();
    }
}
