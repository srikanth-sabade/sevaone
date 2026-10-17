namespace SevaOne.Business.AiChat.DTOs;

/// <summary>
/// Controller-bound chat request. Deliberately has NO SevaOneUserId property at all -- that's
/// the actual security control: there's no slot for a client to smuggle an identity claim
/// into. AiChatProxyService resolves the real citizen (or leaves it anonymous) server-side.
/// </summary>
public sealed record AiChatMessageRequest(
    string? SessionId,
    string? InputMode,
    string? Language,
    string QueryText,
    string? ServiceId);

public sealed record AiResolvedServiceDto(
    string? DepartmentId,
    string ServiceId,
    string ServiceName,
    string? KannadaServiceName,
    string? ServiceCode);

public sealed record AiCandidateDto(
    string ServiceId,
    string ServiceName,
    string? KannadaServiceName,
    string? ShortDescription,
    string? ServiceCode);

public sealed record AiChatMessageResult(
    string SessionId,
    string ResponseType,
    string Message,
    AiResolvedServiceDto? ResolvedService,
    IReadOnlyList<AiResolvedServiceDto> ResolvedServices,
    IReadOnlyList<AiCandidateDto> Candidates,
    IReadOnlyList<string> References,
    IReadOnlyList<string> SuggestedQuestions,
    string? ServiceCode,
    string? ListedCategory);

public sealed record AsrTranscribeResult(string Transcript, string Language, string Status, string? Message);
