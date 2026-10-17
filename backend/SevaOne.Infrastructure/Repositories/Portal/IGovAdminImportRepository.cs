using SevaOne.Domain.Models;

namespace SevaOne.Infrastructure.Repositories.Portal;

public interface IGovAdminImportRepository
{
    Task<Guid> InsertBatchAsync(Guid id, string sourceFilename, string sourceFormat, string uploadedBy, CancellationToken ct);
    Task UpdateBatchAsync(Guid id, string status, int? rowCount, int? errorCount, int? warningCount, string? publishedBy, CancellationToken ct);
    Task<GovAdminImportBatch?> GetBatchAsync(Guid id, CancellationToken ct);
    Task<Guid> InsertRowAsync(Guid id, Guid batchId, int rowNumber, string rawPayload, CancellationToken ct);
    Task UpdateRowAsync(Guid id, string? action, string validationErrors, string validationWarnings, CancellationToken ct);
    Task<IReadOnlyList<GovAdminImportRow>> ListRowsAsync(Guid batchId, CancellationToken ct);
}
