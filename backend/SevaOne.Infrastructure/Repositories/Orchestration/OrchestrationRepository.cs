using System.Data;
using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Orchestration;

public sealed class OrchestrationRepository : RepositoryBase, IOrchestrationRepository
{
    private readonly IDbConnectionFactory _factory;

    public OrchestrationRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task SubmitApplicationAsync(ServiceApplication application, ApplicationEvent firstEvent, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM orchestration.sp_submit_application(
                @Id, @SevaReference, @DepartmentReference, @CitizenId, @SchemeId, @DepartmentId, @Status,
                @SubmittedAt, @SakalaDueAt, @ApplicantName, @Address, @Purpose, @District, @Taluk,
                @EventId, @EventNote, @EventAt)
            """;
        firstEvent.ApplicationId = application.Id;

        var parameters = new DynamicParameters(application);
        parameters.Add("EventId", firstEvent.Id);
        parameters.Add("EventNote", firstEvent.Note);
        parameters.Add("EventAt", firstEvent.At);

        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, parameters, tx, cancellationToken: ct);
            await conn.QuerySingleAsync<WriteResult>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<ServiceApplication?> GetApplicationByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_application_by_id(@id)",
            new { id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ServiceApplication>(cmd);
    }

    public async Task<ServiceApplication?> GetApplicationByReferenceAsync(string sevaReference, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_application_by_reference(@sevaReference)",
            new { sevaReference }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ServiceApplication>(cmd);
    }

    public async Task<IReadOnlyList<ServiceApplication>> GetApplicationsByCitizenAsync(Guid citizenId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_applications_by_citizen(@citizenId)",
            new { citizenId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ServiceApplication>(cmd);
        return rows.AsList();
    }

    public async Task ReassignApplicationsAsync(Guid fromCitizenId, Guid toCitizenId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_reassign_applications(@fromCitizenId, @toCitizenId)",
            new { fromCitizenId, toCitizenId }, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }

    public async Task<ApplicationWithNames?> GetApplicationWithNamesForCitizenAsync(Guid citizenId, string reference, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_application_with_names_for_citizen(@citizenId, @reference)",
            new { citizenId, reference }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ApplicationWithNames>(cmd);
    }

    public async Task<ApplicationWithNames?> GetApplicationWithNamesByReferenceAsync(string reference, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_application_with_names_by_reference(@reference)",
            new { reference }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ApplicationWithNames>(cmd);
    }

    public async Task<IReadOnlyList<ApplicationWithNames>> ListApplicationsWithNamesForCitizenAsync(Guid citizenId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_list_applications_with_names_for_citizen(@citizenId)",
            new { citizenId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<ApplicationWithNames>(cmd);
        return rows.AsList();
    }

    public async Task<ServiceApplication?> GetApplicationByDepartmentReferenceAsync(string departmentCode, string departmentReference, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_application_by_department_reference(@departmentCode, @departmentReference)",
            new { departmentCode, departmentReference }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ServiceApplication>(cmd);
    }

    public async Task UpdateApplicationStatusAsync(Guid id, ApplicationStatus status, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_update_application_status(@id, @status)",
            new { id, status = (int)status }, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }

    public async Task<IReadOnlyList<InflightApplication>> GetInflightApplicationsAsync(int batchSize, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_inflight_applications(@batchSize)",
            new { batchSize }, cancellationToken: ct);
        var rows = await conn.QueryAsync<InflightApplication>(cmd);
        return rows.AsList();
    }

    public async Task InsertDepartmentSessionAsync(DepartmentSession session, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM orchestration.sp_insert_department_session(
                @Id, @CitizenId, @DepartmentId, @EncryptedPayload, @IssuedAt, @ExpiresAt)
            """;
        using var conn = _factory.CreateConnection();
        await conn.QuerySingleAsync<WriteResult>(new CommandDefinition(sql, session, cancellationToken: ct));
    }

    public async Task<DepartmentSession?> GetActiveDepartmentSessionAsync(Guid citizenId, Guid departmentId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_active_department_session(@citizenId, @departmentId)",
            new { citizenId, departmentId }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<DepartmentSession>(cmd);
    }

    public async Task<IReadOnlyList<DepartmentSession>> GetActiveDepartmentSessionsByCitizenAsync(Guid citizenId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_active_department_sessions_by_citizen(@citizenId)",
            new { citizenId }, cancellationToken: ct);
        var rows = await conn.QueryAsync<DepartmentSession>(cmd);
        return rows.AsList();
    }

    public async Task ExpireDepartmentSessionAsync(Guid id, DateTimeOffset expiresAt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_expire_department_session(@id, @expiresAt)",
            new { id, expiresAt }, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }

    public async Task UpsertDeptUserRefAsync(DeptUserRef reference, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM orchestration.sp_upsert_dept_user_ref(
                @Id, @DepartmentId, @CitizenId, @OperatorId, @AssistedMode, @CreatedAt)
            """;
        using var conn = _factory.CreateConnection();
        await conn.QuerySingleAsync<WriteResult>(new CommandDefinition(sql, reference, cancellationToken: ct));
    }

    public async Task InsertWalletTxnAsync(OperatorWalletTxn txn, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM orchestration.sp_insert_wallet_txn(
                @Id, @OperatorId, @CitizenId, @DepartmentId, @SchemeId, @Amount, @TxnStatus,
                @SevaOneTokenId, @IdempotencyKey, @CreatedAt)
            """;
        // Passing `txn` directly leaves TxnStatus (a WalletTxnStatus enum) bound as its
        // underlying int by Npgsql's own enum handling, which runs ahead of Dapper's
        // EnumStringTypeHandler<T> for POCO-sourced parameters -- the same 42883 "function
        // ... does not exist" this codebase already hits on sp_insert_payment_status_post's
        // matching `character varying` PaymentStatus parameter. Same fix: pin it explicitly.
        var parameters = new DynamicParameters(txn);
        parameters.Add("TxnStatus", txn.TxnStatus.ToString(), DbType.String);
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, parameters, tx, cancellationToken: ct);
            await conn.QuerySingleAsync<WriteResult>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<OperatorWalletTxn?> GetWalletTxnByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_wallet_txn_by_idempotency_key(@idempotencyKey)",
            new { idempotencyKey }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<OperatorWalletTxn>(cmd);
    }

    public async Task InsertOutboxMessageAsync(OutboxMessage message, IUnitOfWork? uow, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM orchestration.sp_insert_outbox_message(
                @Id, @Topic, @Payload, @CreatedAt, @PublishedAt, @Attempts, @LastError)
            """;
        var (conn, tx, owned) = await ResolveAsync(uow, ct);
        try
        {
            var cmd = new CommandDefinition(sql, message, tx, cancellationToken: ct);
            await conn.QuerySingleAsync<WriteResult>(cmd);
        }
        finally
        {
            if (owned) await conn.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(int maxAttempts, int batchSize, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_get_pending_outbox_messages(@maxAttempts, @batchSize)",
            new { maxAttempts, batchSize }, cancellationToken: ct);
        var rows = await conn.QueryAsync<OutboxMessage>(cmd);
        return rows.AsList();
    }

    public async Task MarkOutboxPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_mark_outbox_published(@id, @publishedAt)",
            new { id, publishedAt }, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }

    public async Task MarkOutboxFailedAsync(Guid id, int attempts, string error, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM orchestration.sp_mark_outbox_failed(@id, @attempts, @error)",
            new { id, attempts, error }, cancellationToken: ct);
        await conn.QuerySingleAsync<WriteResult>(cmd);
    }
}
