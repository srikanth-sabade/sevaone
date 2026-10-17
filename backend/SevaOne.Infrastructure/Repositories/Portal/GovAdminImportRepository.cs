using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Portal;

public sealed class GovAdminImportRepository : IGovAdminImportRepository
{
    private readonly IDbConnectionFactory _factory;

    public GovAdminImportRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Guid> InsertBatchAsync(Guid id, string sourceFilename, string sourceFormat, string uploadedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<Guid>(new CommandDefinition(
            "SELECT * FROM portal.sp_insert_gov_admin_import_batch(@id, @sourceFilename, @sourceFormat, @uploadedBy)",
            new { id, sourceFilename, sourceFormat, uploadedBy }, cancellationToken: ct));
    }

    public async Task UpdateBatchAsync(Guid id, string status, int? rowCount, int? errorCount, int? warningCount, string? publishedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT * FROM portal.sp_update_gov_admin_import_batch(@id, @status, @rowCount, @errorCount, @warningCount, @publishedBy)",
            new { id, status, rowCount, errorCount, warningCount, publishedBy }, cancellationToken: ct));
    }

    public async Task<GovAdminImportBatch?> GetBatchAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<GovAdminImportBatch>(new CommandDefinition(
            "SELECT * FROM portal.sp_get_gov_admin_import_batch(@id)", new { id }, cancellationToken: ct));
    }

    public async Task<Guid> InsertRowAsync(Guid id, Guid batchId, int rowNumber, string rawPayload, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<Guid>(new CommandDefinition(
            "SELECT * FROM portal.sp_insert_gov_admin_import_row(@id, @batchId, @rowNumber, @rawPayload)",
            new { id, batchId, rowNumber, rawPayload }, cancellationToken: ct));
    }

    public async Task UpdateRowAsync(Guid id, string? action, string validationErrors, string validationWarnings, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT * FROM portal.sp_update_gov_admin_import_row(@id, @action, @validationErrors, @validationWarnings)",
            new { id, action, validationErrors, validationWarnings }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<GovAdminImportRow>> ListRowsAsync(Guid batchId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<GovAdminImportRow>(new CommandDefinition(
            "SELECT * FROM portal.sp_list_gov_admin_import_rows(@batchId)", new { batchId }, cancellationToken: ct));
        return rows.AsList();
    }
}
