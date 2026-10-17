using System.Text.Json;
using System.Text.Json.Serialization;

namespace SevaOne.Infrastructure.AiBackend;

/// <summary>
/// Wire client for ai_backend's citizen-facing endpoints (POST /api/v1/ai-chat/message,
/// POST /api/v1/language/asr) -- see ai_backend/app/schemas/chat.py and
/// ai_backend/app/api/routes/language.py for the source of truth these records mirror.
/// </summary>
public interface IAiBackendClient
{
    Task<AiChatWireResponse> SendChatMessageAsync(AiChatWireRequest request, CancellationToken ct);

    Task<AsrWireResponse> TranscribeAsync(AsrWireRequest request, CancellationToken ct);
}

public sealed record AiBackendHealthWireResponse(string Status, string Service);

public sealed record AiBackendReadyWireResponse(
    string Status,
    [property: JsonPropertyName("active_knowledge_source")] string ActiveKnowledgeSource,
    [property: JsonPropertyName("service_index_prefix")] string ServiceIndexPrefix,
    [property: JsonPropertyName("total_services_indexed")] int TotalServicesIndexed,
    [property: JsonPropertyName("embedding_dimension")] int EmbeddingDimension);

public sealed record AiBackendRefreshWireResponse(string Status, string Message, JsonElement Summary);

/// <summary>Matches ai_backend's ChatRequest exactly (chat.py) -- all camelCase, no attributes
/// needed under JsonSerializerDefaults.Web. SevaOneUserId is the one field the bff always
/// controls: never populated from anything client-supplied, see AiChatProxyService.</summary>
public sealed record AiChatWireRequest(
    string? SessionId,
    string InputMode,
    string Language,
    string QueryText,
    long? SevaOneUserId,
    string? ServiceId);

public sealed record ResolvedServiceWire(
    string? DepartmentId,
    string ServiceId,
    string ServiceName,
    string? KannadaServiceName,
    string? ServiceCode);

public sealed record CandidateWire(
    string ServiceId,
    string ServiceName,
    string? KannadaServiceName,
    string? ShortDescription,
    string? ServiceCode);

/// <summary>ResponseType stays a plain string (Clarify|Resolved|Fallback|NoMatch|Greeting|List|Info)
/// rather than a C# enum -- the bff only relays it to the frontend, never branches on it.</summary>
public sealed record AiChatWireResponse(
    string SessionId,
    string ResponseType,
    string Message,
    ResolvedServiceWire? ResolvedService,
    IReadOnlyList<ResolvedServiceWire> ResolvedServices,
    IReadOnlyList<CandidateWire> Candidates,
    IReadOnlyList<string> References,
    IReadOnlyList<string> SuggestedQuestions,
    string? ServiceCode,
    string? ListedCategory);

/// <summary>Matches ai_backend's AsrJsonRequest (language.py) -- deliberately snake_case field
/// names on the wire, unlike the rest of ai_backend's API, so these three need explicit
/// JsonPropertyName; JsonSerializerDefaults.Web's camelCase policy would otherwise produce
/// "audioBase64"/"audioFormat", which ai_backend's Pydantic model would not recognize.</summary>
public sealed record AsrWireRequest(
    [property: JsonPropertyName("audio_base64")] string AudioBase64,
    [property: JsonPropertyName("audio_format")] string AudioFormat,
    string Language);

public sealed record AsrWireResponse(string Transcript, string Language, string Status, string? Message);
