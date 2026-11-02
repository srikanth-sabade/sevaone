using SevaOne.Business.AiChat.DTOs;

namespace SevaOne.Business.AiChat.Interfaces;

public interface IAiChatProxyService
{
    /// <summary>Forwards to ai_backend, injecting the real logged-in citizen's SevaOneUserId
    /// (looked up server-side from ICurrentCitizen) when authenticated, or null for an
    /// anonymous chat -- never trusts anything client-supplied for identity.</summary>
    Task<AiChatMessageResult> SendMessageAsync(AiChatMessageRequest request, CancellationToken ct);

    /// <summary>Pure passthrough to ai_backend's ASR endpoint -- no identity injection, that
    /// contract has no identity field at all.</summary>
    Task<AsrTranscribeResult> TranscribeAsync(byte[] audioBytes, string? audioFormat, string? language, CancellationToken ct);
}
