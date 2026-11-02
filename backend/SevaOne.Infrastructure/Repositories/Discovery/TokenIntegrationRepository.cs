using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Infrastructure.Repositories.Common;

namespace SevaOne.Infrastructure.Repositories.Discovery;

public sealed class TokenIntegrationRepository : RepositoryBase, ITokenIntegrationRepository
{
    private readonly IDbConnectionFactory _factory;

    public TokenIntegrationRepository(IDbConnectionFactory factory) : base(factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<TokenIntegrationTypeRow>> ListTypesAsync(CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition("SELECT * FROM discovery.sp_list_token_integration_types()", cancellationToken: ct);
        return (await conn.QueryAsync<TokenIntegrationTypeRow>(cmd)).AsList();
    }

    public async Task<IReadOnlyList<TokenIntegrationListRow>> ListServicesAsync(
        string? search, string? departmentCode, bool configuredOnly, bool citizenVisibleOnly, int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_token_integration_services(@search, @departmentCode, @configuredOnly, @citizenVisibleOnly, @limit)",
            new { search, departmentCode, configuredOnly, citizenVisibleOnly, limit }, cancellationToken: ct);
        return (await conn.QueryAsync<TokenIntegrationListRow>(cmd)).AsList();
    }

    public async Task<TokenIntegrationRow?> GetAsync(Guid serviceId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_integration(@serviceId)", new { serviceId }, cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<TokenIntegrationRow>(cmd);
    }

    public async Task UpsertAsync(TokenIntegrationWrite write, string updatedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            """
            SELECT * FROM discovery.sp_upsert_token_integration(
                @ServiceId, @TypeKey, @LandingUrl, @SendMobile, @SendDigilockerId, @StatusUrl,
                @AllowedIps, @ExtraConfig, @IsEnabled, @updatedBy, @StatusSecretCiphertext)
            """,
            new
            {
                write.ServiceId, write.TypeKey, write.LandingUrl, write.SendMobile, write.SendDigilockerId,
                write.StatusUrl, write.AllowedIps, write.ExtraConfig, write.IsEnabled, updatedBy, write.StatusSecretCiphertext,
            },
            cancellationToken: ct);
        await conn.ExecuteAsync(cmd);
    }

    public async Task SetSevasindhuServiceIdAsync(Guid serviceId, string? value, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT discovery.sp_set_service_sevasindhu_id(@serviceId, @value)", new { serviceId, value }, cancellationToken: ct);
        await conn.ExecuteAsync(cmd);
    }

    public async Task<bool> DeleteAsync(Guid serviceId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_delete_token_integration(@serviceId)", new { serviceId }, cancellationToken: ct);
        return await conn.QuerySingleAsync<bool>(cmd);
    }

    public async Task<TokenIntegrationActiveRow?> GetEnabledAsync(Guid serviceId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_enabled_token_integration(@serviceId)", new { serviceId }, cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<TokenIntegrationActiveRow>(cmd);
    }

    public async Task<string> CreateAttemptAsync(TokenIntegrationAttemptWrite attempt, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            """
            SELECT * FROM discovery.sp_create_token_integration_attempt(
                @ServiceId, @DepartmentId, @CitizenId, @SevaOneUserId, @TokenId, @AssistedMode,
                @LandingUrl, @SentFields, @ExpiresAt, @IsSandbox, @EdcsApplicationRefNo)
            """,
            attempt, cancellationToken: ct);
        return await conn.QuerySingleAsync<string>(cmd);
    }

    public async Task<TokenIntegrationAttemptRow?> GetAttemptAsync(string edcsApplicationRefNo, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_integration_attempt(@edcsApplicationRefNo)", new { edcsApplicationRefNo }, cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<TokenIntegrationAttemptRow>(cmd);
    }

    public async Task<TokenIntegrationAckResult> ApplyAcknowledgementAsync(TokenIntegrationAckWrite write, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            """
            SELECT * FROM discovery.sp_apply_token_integration_ack(
                @AttemptId, @DepartmentApplicationRefNo, @PaymentStatus, @Fee, @FeeCollectionDate, @PaymentRefNumber,
                @FileType, @AcknowledgmentDate, @Acknowledgment, @Payload, @PayloadHash, @CallerIp, @TokenVerified)
            """,
            write, cancellationToken: ct);
        return await conn.QuerySingleAsync<TokenIntegrationAckResult>(cmd);
    }

    public async Task<TokenVerifyInfoRow?> GetVerifyInfoAsync(string edcsApplicationRefNo, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TokenVerifyInfoRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_verify_info(@edcsApplicationRefNo)", new { edcsApplicationRefNo }, cancellationToken: ct));
    }

    public async Task<bool> MarkTokenVerifiedAsync(Guid attemptId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<bool>(new CommandDefinition(
            "SELECT * FROM discovery.sp_mark_token_verified(@attemptId)", new { attemptId }, cancellationToken: ct));
    }

    public async Task<TokenApplicationFlags> GetApplicationFlagsAsync(string edcsApplicationRefNo, Guid citizenId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_application_flags(@edcsApplicationRefNo, @citizenId)",
            new { edcsApplicationRefNo, citizenId }, cancellationToken: ct);
        return await conn.QuerySingleAsync<TokenApplicationFlags>(cmd);
    }

    public async Task<TokenStatusTargetRow?> GetStatusTargetAsync(string edcsApplicationRefNo, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_status_target(@edcsApplicationRefNo)", new { edcsApplicationRefNo }, cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<TokenStatusTargetRow>(cmd);
    }

    public async Task<string?> GetStatusSecretCiphertextAsync(Guid serviceId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT discovery.sp_get_token_status_secret(@serviceId)", new { serviceId }, cancellationToken: ct));
    }

    public async Task RecordStatusCheckAsync(Guid attemptId, string? statusText, string? error, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT * FROM discovery.sp_record_token_status_check(@attemptId, @statusText, @error)",
            new { attemptId, statusText, error }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<string>> ListStatusDueAsync(TimeSpan olderThan, int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT \"EdcsApplicationRefNo\" FROM discovery.sp_list_token_status_due(@olderThan, @limit)",
            new { olderThan, limit }, cancellationToken: ct);
        return (await conn.QueryAsync<string>(cmd)).AsList();
    }

    public async Task<IReadOnlyList<TokenAckDueRow>> ListAckDueAsync(TimeSpan olderThan, int limit, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_token_ack_due(@olderThan, @limit)", new { olderThan, limit }, cancellationToken: ct);
        return (await conn.QueryAsync<TokenAckDueRow>(cmd)).AsList();
    }

    public async Task RecordAckCheckAsync(Guid attemptId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition("SELECT discovery.sp_record_token_ack_check(@attemptId)", new { attemptId }, cancellationToken: ct));
    }

    public async Task<TokenIntegrationFileRow?> GetAcknowledgementFileAsync(string edcsApplicationRefNo, Guid citizenId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_integration_acknowledgement(@edcsApplicationRefNo, @citizenId)",
            new { edcsApplicationRefNo, citizenId }, cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<TokenIntegrationFileRow>(cmd);
    }

    public async Task<TokenIntegrationCredentialRow?> GetCredentialAsync(string username, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_integration_credential(@username)", new { username }, cancellationToken: ct);
        return await conn.QuerySingleOrDefaultAsync<TokenIntegrationCredentialRow>(cmd);
    }

    public async Task TouchCredentialAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition("SELECT * FROM discovery.sp_touch_token_integration_credential(@id)", new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<TokenIntegrationCredentialListRow>> ListCredentialsAsync(Guid departmentId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_token_integration_credentials(@departmentId)", new { departmentId }, cancellationToken: ct);
        return (await conn.QueryAsync<TokenIntegrationCredentialListRow>(cmd)).AsList();
    }

    public async Task<Guid> CreateCredentialAsync(Guid departmentId, string username, string passwordHash, string createdBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_create_token_integration_credential(@departmentId, @username, @passwordHash, @createdBy)",
            new { departmentId, username, passwordHash, createdBy }, cancellationToken: ct);
        return await conn.QuerySingleAsync<Guid>(cmd);
    }

    public async Task<bool> UpdateCredentialAsync(Guid id, Guid departmentId, string? passwordHash, bool? isActive, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_update_token_integration_credential(@id, @departmentId, @passwordHash, @isActive)",
            new { id, departmentId, passwordHash, isActive }, cancellationToken: ct);
        return await conn.QuerySingleAsync<bool>(cmd);
    }

    public async Task<TokenProviderRow?> GetProviderAsync(string typeKey, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TokenProviderRow>(new CommandDefinition(
            "SELECT * FROM discovery.sp_get_token_provider(@typeKey)", new { typeKey }, cancellationToken: ct));
    }

    public async Task SaveProviderAsync(string typeKey, string settingsJson, string? secretCiphertext, string updatedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT * FROM discovery.sp_save_token_provider(@typeKey, @settingsJson, @secretCiphertext, @updatedBy)",
            new { typeKey, settingsJson, secretCiphertext, updatedBy }, cancellationToken: ct));
    }

    public async Task FinishAttemptAsync(Guid attemptId, string? landingUrl, string status, string? error, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT * FROM discovery.sp_finish_token_integration_attempt(@attemptId, @landingUrl, @status, @error)",
            new { attemptId, landingUrl, status, error }, cancellationToken: ct));
    }
}
