using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.AiBackend;

public sealed class AiBackendClient : IAiBackendClient
{
    private const string SafeFailureMessage = "AI service is temporarily unavailable.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AiBackendOptions _options;
    private readonly ILogger<AiBackendClient> _log;

    public AiBackendClient(HttpClient http, IOptions<AiBackendOptions> options, ILogger<AiBackendClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public Task<AiChatWireResponse> SendChatMessageAsync(AiChatWireRequest request, CancellationToken ct) =>
        PostAsync<AiChatWireRequest, AiChatWireResponse>("api/v1/ai-chat/message", request, "ai-chat message", ct);

    public Task<AsrWireResponse> TranscribeAsync(AsrWireRequest request, CancellationToken ct) =>
        PostAsync<AsrWireRequest, AsrWireResponse>("api/v1/language/asr", request, "ASR transcription", ct);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string relativePath, TRequest body, string what, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/{relativePath}";

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(url, body, JsonOptions, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // HttpClient.Timeout fires as a TaskCanceledException, indistinguishable from the
            // caller's own cancellation by type alone -- only treat it as "our timeout" when the
            // caller's own token was NOT the one that fired.
            _log.LogWarning("ai_backend {What} timed out ({Url})", what, url);
            throw new AiBackendUnavailableException(SafeFailureMessage, isTimeout: true);
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "ai_backend {What} request failed ({Url})", what, url);
            throw new AiBackendUnavailableException(SafeFailureMessage, isTimeout: false, ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("ai_backend {What} returned {StatusCode} ({Url})", what, (int)response.StatusCode, url);
                throw new AiBackendUnavailableException(SafeFailureMessage, isTimeout: false);
            }

            try
            {
                var parsed = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
                if (parsed is null)
                {
                    _log.LogWarning("ai_backend {What} returned an empty/null body ({Url})", what, url);
                    throw new AiBackendUnavailableException(SafeFailureMessage, isTimeout: false);
                }
                return parsed;
            }
            catch (JsonException ex)
            {
                _log.LogWarning(ex, "ai_backend {What} returned unparseable JSON ({Url})", what, url);
                throw new AiBackendUnavailableException(SafeFailureMessage, isTimeout: false, ex);
            }
        }
    }
}
