using Dapper;
using SevaOne.Domain.Connection;
using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Discovery;

/// <summary>Plain per-call connections throughout — staging writes never need to share a transaction with the live-table upserts in IDiscoveryRepository (a batch is inserted and staged well before publish even begins), so this repository does not need RepositoryBase/IUnitOfWork.</summary>
public sealed class CatalogIngestionRepository : ICatalogIngestionRepository
{
    private readonly IDbConnectionFactory _factory;

    public CatalogIngestionRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Guid> InsertImportBatchAsync(Guid id, string sourceFilename, string sourceFormat, string uploadedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_insert_import_batch(@id, @sourceFilename, @sourceFormat, @uploadedBy)",
            new { id, sourceFilename, sourceFormat, uploadedBy }, cancellationToken: ct);
        return await conn.QuerySingleAsync<Guid>(cmd);
    }

    public async Task UpdateImportBatchStatusAsync(Guid id, string status, int? rowCount, int? errorCount, int? warningCount, string? publishedBy, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_update_import_batch_status(@id, @status, @rowCount, @errorCount, @warningCount, @publishedBy)",
            new { id, status, rowCount, errorCount, warningCount, publishedBy }, cancellationToken: ct);
        await conn.ExecuteAsync(cmd);
    }

    public async Task<CatalogImportBatch?> GetImportBatchAsync(Guid id, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_get_import_batch(@id)",
            new { id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<CatalogImportBatch>(cmd);
    }

    public async Task<Guid> InsertImportRowAsync(Guid id, Guid batchId, int rowNumber, string rawPayload, string? normalizedPayload, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_insert_import_row(@id, @batchId, @rowNumber, @rawPayload, @normalizedPayload)",
            new { id, batchId, rowNumber, rawPayload, normalizedPayload }, cancellationToken: ct);
        return await conn.QuerySingleAsync<Guid>(cmd);
    }

    public async Task UpdateImportRowValidationAsync(Guid id, string? action, string? validationErrors, string? validationWarnings, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_update_import_row_validation(@id, @action, @validationErrors, @validationWarnings)",
            new { id, action, validationErrors, validationWarnings }, cancellationToken: ct);
        await conn.ExecuteAsync(cmd);
    }

    public async Task<IReadOnlyList<CatalogImportRow>> ListImportRowsAsync(Guid batchId, string? action, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        var cmd = new CommandDefinition(
            "SELECT * FROM discovery.sp_list_import_rows(@batchId, @action)",
            new { batchId, action }, cancellationToken: ct);
        var rows = await conn.QueryAsync<CatalogImportRow>(cmd);
        return rows.AsList();
    }
}
