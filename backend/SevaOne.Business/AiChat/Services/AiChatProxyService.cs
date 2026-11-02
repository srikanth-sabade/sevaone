using SevaOne.Business.AiChat.DTOs;
using SevaOne.Business.AiChat.Interfaces;
using SevaOne.Business.Common;
using SevaOne.Infrastructure.AiBackend;
using SevaOne.Infrastructure.Repositories.Portal;

namespace SevaOne.Business.AiChat.Services;

public sealed class AiChatProxyService : IAiChatProxyService
{
    private readonly IAiBackendClient _aiBackend;
    private readonly ICurrentCitizen _current;
    private readonly IPortalRepository _portal;

    public AiChatProxyService(IAiBackendClient aiBackend, ICurrentCitizen current, IPortalRepository portal)
    {
        _aiBackend = aiBackend;
        _current = current;
        _portal = portal;
    }

    public async Task<AiChatMessageResult> SendMessageAsync(AiChatMessageRequest request, CancellationToken ct)
    {
        long? sevaOneUserId = null;
        if (_current.IsAuthenticated && _current.CitizenId is { } citizenId)
        {
            // A citizen row missing here (shouldn't happen for an authenticated session, but
            // not worth failing an AI chat turn over) just degrades to an anonymous-looking
            // request rather than throwing.
            var citizen = await _portal.GetCitizenByIdAsync(citizenId, ct);
            sevaOneUserId = citizen?.SevaOneUserId;
        }

        var wireRequest = new AiChatWireRequest(
            request.SessionId,
            string.IsNullOrWhiteSpace(request.InputMode) ? "Text" : request.InputMode,
            string.IsNullOrWhiteSpace(request.Language) ? "EN" : request.Language,
            request.QueryText,
            sevaOneUserId,
            request.ServiceId);

        var response = await _aiBackend.SendChatMessageAsync(wireRequest, ct);

        return new AiChatMessageResult(
            response.SessionId,
            response.ResponseType,
            response.Message,
            ToDto(response.ResolvedService),
            response.ResolvedServices.Select(ToDto).Where(d => d is not null).Select(d => d!).ToList(),
            response.Candidates.Select(c => new AiCandidateDto(c.ServiceId, c.ServiceName, c.KannadaServiceName, c.ShortDescription, c.ServiceCode)).ToList(),
            response.References,
            response.SuggestedQuestions,
            response.ServiceCode,
            response.ListedCategory);
    }

    public async Task<AsrTranscribeResult> TranscribeAsync(byte[] audioBytes, string? audioFormat, string? language, CancellationToken ct)
    {
        var wireRequest = new AsrWireRequest(
            Convert.ToBase64String(audioBytes),
            string.IsNullOrWhiteSpace(audioFormat) ? "webm" : audioFormat,
            string.IsNullOrWhiteSpace(language) ? "kn" : language);

        var response = await _aiBackend.TranscribeAsync(wireRequest, ct);
        return new AsrTranscribeResult(response.Transcript, response.Language, response.Status, response.Message);
    }

    private static AiResolvedServiceDto? ToDto(ResolvedServiceWire? w) =>
        w is null ? null : new AiResolvedServiceDto(w.DepartmentId, w.ServiceId, w.ServiceName, w.KannadaServiceName, w.ServiceCode);
}
