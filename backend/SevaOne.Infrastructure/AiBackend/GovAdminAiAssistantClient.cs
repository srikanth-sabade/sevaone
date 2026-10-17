using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.AiBackend;

public sealed class GovAdminAiAssistantClient : IGovAdminAiAssistantClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly GovAdminAiAssistantOptions _options;
    private readonly ILogger<GovAdminAiAssistantClient> _log;

    public GovAdminAiAssistantClient(HttpClient http, IOptions<GovAdminAiAssistantOptions> options, ILogger<GovAdminAiAssistantClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<GovAdminAiAskResponse> AskAsync(string message, string sessionCookie, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/ask";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { message })
        };
        if (!string.IsNullOrWhiteSpace(sessionCookie))
            request.Headers.TryAddWithoutValidation("X-Seva-Gov-Session-Cookie", sessionCookie);

        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Government AI assistant returned {StatusCode} ({Url})", (int)response.StatusCode, url);
                throw new AiBackendUnavailableException("Government AI assistant is temporarily unavailable.", false);
            }

            var result = await response.Content.ReadFromJsonAsync<GovAdminAiAskResponse>(JsonOptions, ct);
            return result ?? throw new AiBackendUnavailableException("Government AI assistant returned an empty response.", false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiBackendUnavailableException("Government AI assistant timed out.", true);
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "Government AI assistant request failed ({Url})", url);
            throw new AiBackendUnavailableException("Government AI assistant is temporarily unavailable.", false, ex);
        }
        catch (JsonException ex)
        {
            _log.LogWarning(ex, "Government AI assistant returned invalid JSON ({Url})", url);
            throw new AiBackendUnavailableException("Government AI assistant returned an invalid response.", false, ex);
        }
    }
}
