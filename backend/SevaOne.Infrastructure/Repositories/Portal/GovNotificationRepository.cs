using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Portal;

public sealed class GovNotificationRepository : RepositoryBase, IGovNotificationRepository
{
    private readonly IDbConnectionFactory _factory;

    public GovNotificationRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<GovNotification>> ListAsync(bool activeOnly, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_list_gov_notifications(@ActiveOnly)",
            new { ActiveOnly = activeOnly }, cancellationToken: ct);
        var rows = await conn.QueryAsync<GovNotification>(cmd);
        return rows.AsList();
    }

    public async Task<GovNotification> CreateAsync(GovNotification notification, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM portal.sp_create_gov_notification(@Title, @Body, @CreatedBy)",
            new { notification.Title, notification.Body, notification.CreatedBy }, cancellationToken: ct);
        return await conn.QuerySingleAsync<GovNotification>(cmd);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, Guid? updatedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT portal.sp_set_gov_notification_active(@Id, @IsActive, @UpdatedBy)",
            new { Id = id, IsActive = isActive, UpdatedBy = updatedBy }, cancellationToken: ct);
        await conn.ExecuteScalarAsync(cmd);
    }
}
