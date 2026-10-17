using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Real ISmsOtpSender for Karnataka's Mobile Seva SMS gateway. Adapted from the department's
/// own Java/C# sample (dept-provided, not in this repo's history before now) -- see
/// MobileSevaSmsOptions for where the credentials/template live.
///
/// ASSUMPTION, NOT CONFIRMED: the sample calls two helper methods, `MD5(password)` and
/// `hashGenerator(username, senderId, message, secureKey)`, without showing either
/// implementation -- they were black-box calls in every sample given. `MD5(password)` is
/// unambiguous (a plain hex MD5 digest is the standard reading of a bare "MD5(x)" call, and
/// widely used for this style of older gateway). `hashGenerator` is not: its algorithm, field
/// order, and any delimiter between fields are genuinely unknown. HashGenerator() below guesses
/// plain MD5 of the four fields concatenated in call order with no delimiter, because that's
/// the simplest reading consistent with the one clue available (the call signature's argument
/// order) -- but this is unverified. If real OTP sends fail (gateway rejects the signature),
/// this is the first thing to revisit; get the actual algorithm from Mobile Seva support and
/// replace just this method.
/// </summary>
public sealed class MobileSevaSmsOtpSender : ISmsOtpSender
{
    private readonly HttpClient _http;
    private readonly MobileSevaSmsOptions _options;
    private readonly ILogger<MobileSevaSmsOtpSender> _log;

    public MobileSevaSmsOtpSender(HttpClient http, IOptions<MobileSevaSmsOptions> options, ILogger<MobileSevaSmsOtpSender> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task SendOtpAsync(string mobile, string otp, CancellationToken ct)
    {
        var message = _options.MessageTemplate.Replace("{#num#}", otp);
        var form = new Dictionary<string, string>
        {
            ["mobileno"] = mobile,
            ["senderid"] = _options.SenderId,
            ["content"] = message,
            ["smsservicetype"] = "otpmsg",
            ["username"] = _options.Username,
            ["password"] = Md5Hex(_options.Password),
            ["key"] = HashGenerator(_options.Username, _options.SenderId, message, _options.SecureKey),
            ["templateid"] = _options.TemplateId
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _log.LogError("Mobile Seva SMS gateway returned {StatusCode} for {Mobile}: {Body}", (int)response.StatusCode, mobile, body);
            throw new InvalidOperationException("Mobile Seva SMS gateway request failed.");
        }

        // Response is a raw "<statusCode>,MsgID = <id>" string (per the dept sample's own doc
        // comment) -- no documented code table was provided, so this only logs it rather than
        // parsing/asserting a specific success code. Revisit once the code table is known.
        _log.LogInformation("Mobile Seva SMS gateway response for {Mobile}: {Body}", mobile, body);
    }

    private static string Md5Hex(string value)
    {
#pragma warning disable CA5351 // MD5 is what the gateway's own sample calls for, not a choice made here
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
#pragma warning restore CA5351
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>See the class doc comment -- this is an unverified guess, not a confirmed algorithm.</summary>
    private static string HashGenerator(string username, string senderId, string message, string secureKey) =>
        Md5Hex(username + senderId + message + secureKey);
}
