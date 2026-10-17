namespace SevaOne.Infrastructure.AiBackend;

/// <summary>
/// Wire client for ai_backend's own ingestion/health endpoints (GET /health, GET /api/v1/ready,
/// POST /api/v1/ai-knowledge/refresh) -- see ai_backend/app/api/routes/health.py and
/// ai_backend/app/api/routes/ingestion.py for the source of truth these records mirror. Kept
/// separate from IAiBackendClient (citizen chat/ASR), which targets sevaone-rag instead.
/// </summary>
public interface IAiIngestionClient
{
    Task<AiBackendHealthWireResponse> GetHealthAsync(CancellationToken ct);

    Task<AiBackendReadyWireResponse> GetReadyAsync(CancellationToken ct);

    Task<AiBackendRefreshWireResponse> RefreshKnowledgeBaseAsync(CancellationToken ct);
}
