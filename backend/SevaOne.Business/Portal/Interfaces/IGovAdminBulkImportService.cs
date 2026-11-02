using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

public interface IGovAdminBulkImportService
{
    Task<Guid> StartImportAsync(string sourceFilename, string sourceFormat, IReadOnlyList<GovAdminImportRowRequest> rows, CancellationToken ct);
    Task<GovAdminImportBatchDto?> GetBatchAsync(Guid batchId, CancellationToken ct);
    Task<IReadOnlyList<GovAdminImportRowDto>> ListRowsAsync(Guid batchId, CancellationToken ct);
    Task<GovAdminImportPublishResult> PublishAsync(Guid batchId, CancellationToken ct);
    Task DiscardAsync(Guid batchId, CancellationToken ct);
}
