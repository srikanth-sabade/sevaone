using System.Data;
using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Status;

public sealed class StatusRepository : IStatusRepository
{
    private readonly IDbConnectionFactory _factory;

    public StatusRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InsertApplicationEventAsync(ApplicationEvent applicationEvent, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM status.sp_insert_application_event(
                @Id, @ApplicationId, @Status, @Note, @At)
            """;
        using var conn = _factory.CreateConnection();
        await conn.QuerySingleAsync<WriteResult>(new CommandDefinition(sql, applicationEvent, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ApplicationEvent>> GetTimelineAsync(Guid applicationId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM status.sp_get_timeline(@applicationId)",
            new { applicationId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ApplicationEvent>(cmd);
        return rows.AsList();
    }

    public async Task<PaymentStatusPost> InsertPaymentStatusPostAsync(PaymentStatusPost post, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM status.sp_insert_payment_status_post(
                @Id, @CitizenId, @DepartmentId, @SchemeId, @SevaOneTokenId, @TokenValue, @TokenHashAtPost,
                @PaymentStatus, @Amount, @PostedAt, @TokenValidTill, @IdempotencyKey)
            """;
        // Passing `post` directly leaves PaymentStatus (a PaymentPostStatus enum) bound as its
        // underlying int by Npgsql's own enum handling, which runs ahead of Dapper's
        // EnumStringTypeHandler<T> for POCO-sourced parameters -- 42883 "function ... does not
        // exist" against sp_insert_payment_status_post's `character varying` parameter.
        // DynamicParameters lets us pin PaymentStatus to its string form explicitly, same as
        // EnumStringTypeHandler already does for named/typed parameters.
        var parameters = new DynamicParameters(post);
        parameters.Add("PaymentStatus", post.PaymentStatus.ToString(), DbType.String);
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(sql, parameters, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
        return post;
    }

    public async Task<PaymentStatusPost?> GetPaymentStatusByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM status.sp_get_payment_status_by_idempotency_key(@idempotencyKey)",
            new { idempotencyKey }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<PaymentStatusPost>(cmd);
    }
}
