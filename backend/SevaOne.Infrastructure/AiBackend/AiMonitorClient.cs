using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.AiBackend;

public sealed class AiMonitorClient : IAiMonitorClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly AiMonitorOptions _options;
    private readonly ILogger<AiMonitorClient> _log;

    public AiMonitorClient(HttpClient http, IOptions<AiMonitorOptions> options, ILogger<AiMonitorClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<AiMonitorMetricsWireResponse?> GetMetricsAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl)) return null;

        var url = $"{_options.BaseUrl.TrimEnd('/')}/metrics";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(_options.ApiKeyFile))
        {
            try { apiKey = await File.ReadAllTextAsync(_options.ApiKeyFile, ct); }
            catch (IOException) { apiKey = string.Empty; }
            catch (UnauthorizedAccessException) { apiKey = string.Empty; }
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Monitor-Key", apiKey.Trim());
        }

        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("AI monitor returned {StatusCode} ({Url})", (int)response.StatusCode, url);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AiMonitorMetricsWireResponse>(JsonOptions, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _log.LogWarning("AI monitor timed out ({Url})", url);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "AI monitor request failed ({Url})", url);
            return null;
        }
        catch (JsonException ex)
        {
            _log.LogWarning(ex, "AI monitor returned unparseable JSON ({Url})", url);
            return null;
        }
    }
}
