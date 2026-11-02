namespace SevaOne.Infrastructure.AiBackend;

/// <summary>
/// The separate Python FastAPI "ai_backend" service (RAG chat/discovery + voice transcription).
/// BaseUrl is a full origin (e.g. "http://172.17.0.1:8000" for the GPU-box SSH tunnel used in
/// this deployment, or "http://ai-backend:8000" if it's ever brought back up as a Docker
/// service on the same network) -- see AiBackendClient for how request paths are appended.
/// </summary>
public sealed class AiBackendOptions
{
    public const string SectionName = "AiBackend";

    public string BaseUrl { get; set; } = "http://localhost:8000";
}
