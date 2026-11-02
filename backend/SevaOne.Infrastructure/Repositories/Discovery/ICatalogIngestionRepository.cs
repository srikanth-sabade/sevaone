using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Discovery;

/// <summary>Data access for the catalog import staging tables (catalog_import_batch/row) — split out from <see cref="IDiscoveryRepository"/>. Never touches the live department/sub-department/service tables; that's <see cref="IDiscoveryRepository"/>'s upsert methods, called separately by CatalogPublishService once a batch is approved.</summary>
public interface ICatalogIngestionRepository
{
    Task<Guid> InsertImportBatchAsync(Guid id, string sourceFilename, string sourceFormat, string uploadedBy, CancellationToken ct);

    /// <summary>Pass null for any count to leave it unchanged — e.g. a publish/discard transition that only flips status.</summary>
    Task UpdateImportBatchStatusAsync(Guid id, string status, int? rowCount, int? errorCount, int? warningCount, string? publishedBy, CancellationToken ct);

    Task<CatalogImportBatch?> GetImportBatchAsync(Guid id, CancellationToken ct);

    Task<Guid> InsertImportRowAsync(Guid id, Guid batchId, int rowNumber, string rawPayload, string? normalizedPayload, CancellationToken ct);

    Task UpdateImportRowValidationAsync(Guid id, string? action, string? validationErrors, string? validationWarnings, CancellationToken ct);

    /// <summary>action=null returns every row in the batch.</summary>
    Task<IReadOnlyList<CatalogImportRow>> ListImportRowsAsync(Guid batchId, string? action, CancellationToken ct);
}
