using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.AiChat.DTOs;
using SevaOne.Business.AiChat.Interfaces;
using SevaOne.Infrastructure.AiBackend;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Proxies the citizen-facing AI chat/discovery feature to the separate ai_backend service,
/// instead of the frontend calling it directly (see frontend/nginx.conf's now-removed
/// /ai-api/ location). The point of this indirection: ai_backend has zero authentication of
/// its own, so it can never be trusted to resolve "who is this citizen" itself -- the bff
/// does that here (AiChatProxyService, from ICurrentCitizen) and injects it server-side.
///
/// Deliberately no [Authorize] on either action -- both must keep working for anonymous
/// citizens, same as today. ICurrentCitizen is populated globally by Wso2CitizenAuthMiddleware
/// whenever the seva.wso2.at cookie is present and valid, independent of any [Authorize] gate.
/// </summary>
[Route("api/v1")]
public sealed class AiChatController : SevaControllerBase
{
    private readonly IAiChatProxyService _aiChat;

    public AiChatController(IAiChatProxyService aiChat)
    {
        _aiChat = aiChat;
    }

    [HttpPost("ai-chat/message")]
    public async Task<IActionResult> Message([FromBody] AiChatMessageRequest body, CancellationToken ct)
    {
        await ValidateAsync(body, ct);
        try
        {
            return Ok(await _aiChat.SendMessageAsync(body, ct));
        }
        catch (AiBackendUnavailableException ex)
        {
            return StatusCode(
                ex.IsTimeout ? StatusCodes.Status504GatewayTimeout : StatusCodes.Status502BadGateway,
                new { title = "AI chat is temporarily unavailable." });
        }
    }

    /// <summary>Not called by the frontend today (the mic button transcribes client-side via
    /// the browser's own SpeechRecognition API) -- built now so the plumbing is ready whenever
    /// that voice flow is redesigned to use ai_backend's server-side Bhashini transcription
    /// instead. RequestSizeLimit gives headroom over ai_backend's own 5MB raw-audio ceiling
    /// (ai_backend/app/config.py's MAX_AUDIO_UPLOAD_SIZE_BYTES) for multipart overhead.</summary>
    [HttpPost("language/asr")]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> Asr(IFormFile file, [FromForm] string? language, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { title = "Empty audio upload." });
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();

        try
        {
            return Ok(await _aiChat.TranscribeAsync(stream.ToArray(), extension, language, ct));
        }
        catch (AiBackendUnavailableException ex)
        {
            return StatusCode(
                ex.IsTimeout ? StatusCodes.Status504GatewayTimeout : StatusCodes.Status502BadGateway,
                new { title = "Voice transcription is temporarily unavailable." });
        }
    }
}
