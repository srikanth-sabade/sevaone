using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.ExternalHttp;

public sealed class TokenStatusHttpClient : ITokenStatusHttpClient
{
    public const string ClientName = "token-status";
    private const int MaxResponseBytes = 256 * 1024;

    private readonly IHttpClientFactory _factory;

    public TokenStatusHttpClient(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<TokenStatusHttpResult> PostJsonAsync(string url, string jsonBody, TokenStatusAuth auth, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return new TokenStatusHttpResult(null, null, "The status URL is not a valid web address.");

        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(jsonBody, Encoding.UTF8, "application/json") };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        switch (auth.AuthType)
        {
            case "basic" when !string.IsNullOrEmpty(auth.User) && auth.Secret is not null:
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", OutboundAddressGuard.BasicHeader(auth.User!, auth.Secret));
                break;
            case "bearer" when !string.IsNullOrEmpty(auth.Secret):
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Secret);
                break;
            case "header" when !string.IsNullOrEmpty(auth.HeaderName) && !string.IsNullOrEmpty(auth.Secret):
                request.Headers.TryAddWithoutValidation(auth.HeaderName!, auth.Secret);
                break;
        }

        return await SendAsync(request, ct);
    }

    public async Task<TokenStatusHttpResult> PostFormAsync(string url, IReadOnlyDictionary<string, string> fields, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return new TokenStatusHttpResult(null, null, "The address is not a valid web address.");

        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new FormUrlEncodedContent(fields) };
        return await SendAsync(request, ct);
    }

    private async Task<TokenStatusHttpResult> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using var client = _factory.CreateClient(ClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > MaxResponseBytes) return new TokenStatusHttpResult((int)response.StatusCode, null, "The department's reply was too large.");
                buffer.Write(chunk, 0, read);
            }
            return new TokenStatusHttpResult((int)response.StatusCode, Encoding.UTF8.GetString(buffer.ToArray()), null);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new TokenStatusHttpResult(null, null, "The department's status API did not answer in time.");
        }
        catch (HttpRequestException e)
        {
            return new TokenStatusHttpResult(null, null, $"Could not reach the department's status API ({e.InnerException?.Message ?? e.Message}).");
        }
    }

    /// <summary>The handler: no redirects, short timeouts, and the address is checked at connect time (after DNS),
    /// so a name that resolves to an internal address is refused too.</summary>
    public static SocketsHttpHandler CreateHandler(IOptions<TokenStatusHttpOptions> options)
    {
        var allowPrivate = options.Value.AllowPrivateStatusHosts;
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            UseProxy = false,
            ConnectCallback = async (context, ct) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
                var allowed = addresses.Where(a => allowPrivate || !OutboundAddressGuard.IsBlocked(a)).ToArray();
                if (allowed.Length == 0) throw new HttpRequestException("The address is not allowed for outbound calls.");

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
    }
}
