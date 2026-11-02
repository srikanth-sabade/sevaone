using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Audit;

public sealed class AuditRepository : IAuditRepository
{
    private readonly IDbConnectionFactory _factory;

    public AuditRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InsertAsync(AuditLog log, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM audit.sp_insert_audit_log(
                @Id, @CitizenId, @Action, @Resource, @Detail, @IpAddress, @At)
            """;
        using var conn = _factory.CreateConnection();
        await conn.QuerySingleAsync<WriteResult>(new CommandDefinition(sql, log, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AuditLog>> GetByCitizenAsync(Guid citizenId, int take, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM audit.sp_get_audit_logs_by_citizen(@citizenId, @take)",
            new { citizenId, take }, cancellationToken: ct);
        var rows = await conn.QueryAsync<AuditLog>(cmd);
        return rows.AsList();
    }
}
