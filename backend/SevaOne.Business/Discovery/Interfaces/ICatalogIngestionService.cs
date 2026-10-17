using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>
/// Parse -> normalize -> validate -> stage. Never touches department_master/
/// sub_department_master/sevaone_service_master directly -- see ICatalogPublishService for
/// that.
/// </summary>
public interface ICatalogIngestionService
{
    /// <summary>Stages every row as a catalog_import_row and moves the batch to "ready_to_publish" (zero rows carry a blocking error) or "failed" (at least one does) -- never "published", that only happens via ICatalogPublishService.</summary>
    Task<Guid> StartImportAsync(string sourceFilename, string sourceFormat, string uploadedBy, IReadOnlyList<CatalogRow> rows, CancellationToken ct);

    Task<ImportBatchStatusDto?> GetBatchAsync(Guid batchId, CancellationToken ct);

    /// <summary>action=null returns every row; pass "conflict"/"insert"/"update"/"unchanged" to filter the review view.</summary>
    Task<IReadOnlyList<ImportRowDto>> ListRowsAsync(Guid batchId, string? action, CancellationToken ct);

    Task DiscardAsync(Guid batchId, CancellationToken ct);
}
