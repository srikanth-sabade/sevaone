using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Notification;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _factory;

    public NotificationRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<NotificationMessage> InsertAsync(NotificationMessage message, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM notification.sp_insert_notification(
                @Id, @CitizenId, @Channel, @Recipient, @Subject, @Body, @CreatedAt, @SentAt)
            """;
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(sql, message, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
        return message;
    }

    public async Task<IReadOnlyList<NotificationMessage>> GetPendingAsync(int batchSize, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM notification.sp_get_pending_notifications(@batchSize)",
            new { batchSize }, cancellationToken: ct);
        var rows = await conn.QueryAsync<NotificationMessage>(cmd);
        return rows.AsList();
    }

    public async Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM notification.sp_mark_notification_sent(@id, @sentAt)",
            new { id, sentAt }, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }
}
