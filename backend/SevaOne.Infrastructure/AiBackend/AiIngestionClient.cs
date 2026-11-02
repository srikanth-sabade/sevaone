using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.AiBackend;

public sealed class AiIngestionClient : IAiIngestionClient
{
    private const string SafeFailureMessage = "AI service is temporarily unavailable.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AiIngestionOptions _options;
    private readonly ILogger<AiIngestionClient> _log;

    public AiIngestionClient(HttpClient http, IOptions<AiIngestionOptions> options, ILogger<AiIngestionClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public Task<AiBackendHealthWireResponse> GetHealthAsync(CancellationToken ct) =>
        GetAsync<AiBackendHealthWireResponse>("health", "health check", ct);

    public Task<AiBackendReadyWireResponse> GetReadyAsync(CancellationToken ct) =>
        GetAsync<AiBackendReadyWireResponse>("api/v1/ready", "readiness check", ct);

    public Task<AiBackendRefreshWireResponse> RefreshKnowledgeBaseAsync(CancellationToken ct) =>
        PostEmptyAsync<AiBackendRefreshWireResponse>("api/v1/ai-knowledge/refresh", "knowledge refresh", ct);

    private async Task<TResponse> GetAsync<TResponse>(string relativePath, string what, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/{relativePath}";
        HttpResponseMessage response;
        try { response = await _http.GetAsync(url, ct); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
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
                if (parsed is null) throw new AiBackendUnavailableException(SafeFailureMessage, isTimeout: false);
                return parsed;
            }
            catch (JsonException ex)
            {
                _log.LogWarning(ex, "ai_backend {What} returned unparseable JSON ({Url})", what, url);
                throw new AiBackendUnavailableException(SafeFailureMessage, isTimeout: false, ex);
            }
        }
    }

    private async Task<TResponse> PostEmptyAsync<TResponse>(string relativePath, string what, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/{relativePath}";
        HttpResponseMessage response;
        try { response = await _http.PostAsync(url, content: null, ct); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
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
