using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SevaOne.Infrastructure.ExternalHttp;

/// <summary>How the status API is authenticated, as set in the admin console.</summary>
public sealed record TokenStatusAuth(string AuthType, string? User, string? HeaderName, string? Secret);

public sealed record TokenStatusHttpResult(int? HttpStatus, string? Body, string? Error);

/// <summary>Calls a department's status API (token integration URL 3). Outbound only, to an address an admin typed in,
/// so it refuses internal addresses, does not follow redirects, and caps time and response size.</summary>
public interface ITokenStatusHttpClient
{
    Task<TokenStatusHttpResult> PostJsonAsync(string url, string jsonBody, TokenStatusAuth auth, CancellationToken ct);

    /// <summary>A browser-style form POST (application/x-www-form-urlencoded), used to check that a department's landing page
    /// accepts our hand-off. Same guards as the JSON call.</summary>
    Task<TokenStatusHttpResult> PostFormAsync(string url, IReadOnlyDictionary<string, string> fields, CancellationToken ct);
}

public sealed class TokenStatusHttpOptions
{
    public const string SectionName = "TokenIntegration";

    /// <summary>For local testing against a department stand-in on a private network. Never on in production.</summary>
    public bool AllowPrivateStatusHosts { get; set; }
}

/// <summary>Which addresses a server-side call must never reach: this machine, private networks, link-local
/// (including cloud metadata at 169.254.169.254) and multicast.</summary>
public static class OutboundAddressGuard
{
    public static bool IsBlocked(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.None)) return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal || address.IsIPv6Multicast || address.IsIPv6Teredo;

        var b = address.GetAddressBytes();
        return b[0] == 0 || b[0] == 10 || b[0] == 127
            || (b[0] == 100 && b[1] is >= 64 and <= 127)       // carrier-grade NAT
            || (b[0] == 169 && b[1] == 254)                     // link-local, cloud metadata
            || (b[0] == 172 && b[1] is >= 16 and <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 192 && b[1] == 0 && b[2] == 0)
            || (b[0] == 198 && b[1] is 18 or 19)                // benchmarking
            || b[0] >= 224;                                      // multicast and reserved
    }

    public static string BasicHeader(string user, string secret) => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{secret}"));
}

/// <summary>The Seva Sindhu EP0115 token API (TokenIntegration type "edcs_api"): where SevaOne asks for a token and application reference.</summary>
public sealed class EdcsApiOptions
{
    public const string SectionName = "TokenIntegration:EdcsApi";

    /// <summary>For example https://sevasindhu.karnataka.gov.in/EP0115_SevaOne_API_UAT/api/TokenIntegration (no trailing slash).</summary>
    public string? BaseUrl { get; set; }
}
