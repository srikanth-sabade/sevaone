namespace SevaOne.Infrastructure.AiBackend;

/// <summary>
/// The original ai_backend service (RAG ingestion, health/readiness, knowledge-base refresh),
/// still reached at its GPU-box tunnel port 8000 even after citizen chat moved to the "merged"
/// sevaone-rag service (see AiBackendOptions). Only ai_backend implements /api/v1/ready and
/// /api/v1/ai-knowledge/refresh -- sevaone-rag does not -- so the admin panel's AI status/
/// reingest calls must always target this service, independent of AiBackendOptions.BaseUrl.
/// </summary>
public sealed class AiIngestionOptions
{
    public const string SectionName = "AiIngestion";

    public string BaseUrl { get; set; } = "http://localhost:8000";
}
