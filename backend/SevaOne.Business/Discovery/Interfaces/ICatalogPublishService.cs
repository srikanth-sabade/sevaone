using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>
/// Staging -> live tables, in one transaction. See ICatalogIngestionService for how a batch
/// gets to "ready_to_publish" in the first place -- this only ever reads already-staged rows.
/// </summary>
public interface ICatalogPublishService
{
    Task<PublishResult> PublishAsync(Guid batchId, string publishedBy, CancellationToken ct);
}
