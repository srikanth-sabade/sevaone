using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevaOne.Domain.Common;

namespace SevaOne.Infrastructure.ExternalAuth;

public sealed class SevaSindhuCatalog2Client : ISevaSindhuCatalog2Client
{
    private const string SafeFailureMessage = "Unable to open this service right now. Please try again.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SevaSindhuCatalog2Options _options;
    private readonly ILogger<SevaSindhuCatalog2Client> _log;

    public SevaSindhuCatalog2Client(HttpClient http, IOptions<SevaSindhuCatalog2Options> options, ILogger<SevaSindhuCatalog2Client> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<string> GenerateTokenAsync(string serviceId, string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            throw new DomainException("Service ID is required.");
        }
        if (string.IsNullOrWhiteSpace(userId))
        {
            _log.LogError("No operator/applicant ID was resolved -- refusing to call the token API.");
            throw new DomainException(SafeFailureMessage);
        }

        var request = new TokenRequest(_options.AssistedMode, serviceId, userId);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(_options.TokenUrl, request, JsonOptions, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _log.LogWarning("Seva Sindhu Catalog2 token_generation timed out for service {ServiceId}", serviceId);
            throw new DomainException(SafeFailureMessage);
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "Seva Sindhu Catalog2 token_generation request failed for service {ServiceId}", serviceId);
            throw new DomainException(SafeFailureMessage);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning(
                    "Seva Sindhu Catalog2 token_generation returned {StatusCode} for service {ServiceId}",
                    (int)response.StatusCode, serviceId);
                throw new DomainException(SafeFailureMessage);
            }

            TokenResponse? body;
            try
            {
                body = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, ct);
            }
            catch (JsonException ex)
            {
                _log.LogWarning(ex, "Seva Sindhu Catalog2 token_generation returned unparseable JSON for service {ServiceId}", serviceId);
                throw new DomainException(SafeFailureMessage);
            }

            if (body is null || body.StatusCode != "200" || string.IsNullOrWhiteSpace(body.Token))
            {
                _log.LogWarning(
                    "Seva Sindhu Catalog2 token_generation returned an unexpected response for service {ServiceId}: statusCode={StatusCode}, hasToken={HasToken}",
                    serviceId, body?.StatusCode, !string.IsNullOrWhiteSpace(body?.Token));
                throw new DomainException(SafeFailureMessage);
            }

            return body.Token;
        }
    }

    private sealed record TokenRequest(
        [property: JsonPropertyName("Assisted_Mode")] string AssistedMode,
        [property: JsonPropertyName("Service_Id")] string ServiceId,
        [property: JsonPropertyName("User_Id")] string UserId);

    private sealed record TokenResponse(
        [property: JsonPropertyName("statusCode")] string? StatusCode,
        [property: JsonPropertyName("Token")] string? Token);
}
