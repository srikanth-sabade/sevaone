using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Portal;

public sealed class ServiceNoticeRepository : RepositoryBase, IServiceNoticeRepository
{
    private readonly IDbConnectionFactory _factory;

    public ServiceNoticeRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<ServiceNotice?> GetAsync(bool activeOnly, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_get_service_notice(@activeOnly)",
            new { activeOnly }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ServiceNotice>(cmd);
    }

    public async Task<IReadOnlyList<ServiceNoticeHistory>> ListHistoryAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_list_service_notice_history()",
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ServiceNoticeHistory>(cmd);
        return rows.AsList();
    }

    public async Task<ServiceNotice> UpsertAsync(ServiceNotice notice, Guid? updatedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_upsert_service_notice(@Title, @Message, @Severity, @IsActive, @StartsAt, @EndsAt, @UpdatedBy)",
            new
            {
                notice.Title,
                notice.Message,
                notice.Severity,
                notice.IsActive,
                notice.StartsAt,
                notice.EndsAt,
                UpdatedBy = updatedBy
            }, cancellationToken: ct);
        return await conn.QuerySingleAsync<ServiceNotice>(cmd);
    }

    public async Task StopAsync(Guid? stoppedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT portal.sp_delete_service_notice(@StoppedBy)",
            new { StoppedBy = stoppedBy }, cancellationToken: ct);
        await conn.ExecuteScalarAsync(cmd);
    }
}
