using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevaOne.Domain.Common;

namespace SevaOne.Infrastructure.ExternalAuth;

public sealed class DigiLockerClient : IDigiLockerClient
{
    private static readonly JsonSerializerOptions RequestJson = new() { PropertyNamingPolicy = null };
    private static readonly string[] DobFormats = ["ddMMyyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yyyy"];

    private readonly HttpClient _http;
    private readonly DigiLockerOptions _options;
    private readonly ILogger<DigiLockerClient> _log;

    public DigiLockerClient(HttpClient http, IOptions<DigiLockerOptions> options, ILogger<DigiLockerClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<string> StartSignInAsync(string appParam, string flow, CancellationToken ct)
    {
        EnsureConfigured();
        var redirectUrl = (_options.CallbackUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            throw new DomainException("DigiLocker CallbackUrl is not configured.");
        }

        var dlFlow = string.Equals(flow, "signup", StringComparison.OrdinalIgnoreCase) ? "signup" : "";
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var payload = new Dictionary<string, string>
        {
            ["DeptKey"] = _options.DeptKey,
            ["RedirectURL"] = redirectUrl,
            ["AppParam"] = appParam,
            ["DLFLow"] = dlFlow,
            ["code_challange"] = challenge,
            ["code_challange_method"] = "S256",
            ["APIMac"] = ApiMac(_options.DeptKey, appParam, redirectUrl)
        };

        var resp = await PostAsync("DigiLockerSignInWithcodeChallange", payload, ct);
        var obj = ParseRespObject(resp);
        var url = ReadString(obj, "RedirectURL") ?? ReadString(obj, "redirectURL");
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("CeG DigiLocker sign-in did not return RedirectURL.");
        }

        return url;
    }

    public async Task<DigiLockerProfile> GetProfileFromTxnAsync(string appParam, string txnId, CancellationToken ct)
    {
        EnsureConfigured();
        var payload = new Dictionary<string, string>
        {
            ["DeptKey"] = _options.DeptKey,
            ["AppParam"] = appParam,
            ["TxnID"] = txnId,
            ["APIMac"] = ApiMac(_options.DeptKey, appParam, txnId)
        };

        var resp = await PostAsync("GetDigilockerIDFromTxnID", payload, ct);
        var obj = ParseRespObject(resp);
        var id = ReadString(obj, "digilockerid")
                 ?? ReadString(obj, "dgilockerid")
                 ?? ReadString(obj, "DigilockerID");
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ForbiddenException("Could not find DigiLocker id for this transaction.");
        }

        return new DigiLockerProfile
        {
            Id = id,
            Name = ReadString(obj, "name") ?? string.Empty,
            Mobile = ReadString(obj, "mobile"),
            Email = ReadString(obj, "email"),
            DateOfBirth = ParseDob(ReadString(obj, "dob") ?? ReadString(obj, "date_of_birth")),
            Gender = NormalizeGender(ReadString(obj, "gender"))
        };
    }

    public async Task<DigiLockerAadhaarDetails?> GetEAadhaarDetailsAsync(string digiLockerId, CancellationToken ct)
    {
        try
        {
            EnsureConfigured();
            var payload = new Dictionary<string, string>
            {
                ["DeptKey"] = _options.DeptKey,
                ["DigilockerID"] = digiLockerId,
                ["APIMac"] = ApiMac(_options.DeptKey, digiLockerId)
            };

            var resp = await PostAsync("GetEAadharDetails", payload, ct);
            var obj = ParseRespObject(resp);

            // CeG sometimes double-encodes this specific call's RespObj: the outer envelope's
            // RespObj is a JSON string whose own content is a second layer of JSON-quoted,
            // escaped text wrapping the actual eAadhaar XML, rather than a JSON object.
            // ParseRespObject only unwraps one layer, so `obj` can legitimately come back as a
            // JSON string (the XML itself) here -- handle that directly instead of assuming
            // it's always an object and calling TryGetProperty on it (throws InvalidOperationException
            // when the element isn't an object, which is exactly what was happening before this fix).
            if (obj.ValueKind == JsonValueKind.String)
            {
                var xmlDirect = obj.GetString();
                return string.IsNullOrWhiteSpace(xmlDirect) ? null : ParseAadhaarXml(xmlDirect, digiLockerId);
            }

            return ParseAadhaarFromResp(obj, digiLockerId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogWarning(ex, "CeG GetEAadharDetails failed for DigiLocker id {DigiLockerId}", digiLockerId);
            return null;
        }
    }

    public async Task<IReadOnlyList<DigiLockerIssuedDocument>> GetIssuedDocListAsync(string digiLockerId, CancellationToken ct)
    {
        EnsureConfigured();
        var payload = new Dictionary<string, string>
        {
            ["DeptKey"] = _options.DeptKey,
            ["DigilockerID"] = digiLockerId,
            ["APIMac"] = ApiMac(_options.DeptKey, digiLockerId)
        };

        var resp = await PostAsync("GetIssuedDocList", payload, ct);
        if (string.IsNullOrWhiteSpace(resp.RespObj))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(resp.RespObj);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out _))
            {
                throw new InvalidOperationException(ReadString(doc.RootElement, "error_desc") ?? "Error getting file list from DigiLocker.");
            }

            return [];
        }

        var list = new List<DigiLockerIssuedDocument>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            list.Add(new DigiLockerIssuedDocument
            {
                DocumentName = ReadString(item, "DocumentName") ?? string.Empty,
                DocumentType = ReadString(item, "DocumentType") ?? string.Empty,
                DocumentUri = ReadString(item, "DocumentURI") ?? ReadString(item, "DocumentUri") ?? string.Empty,
                DocumentDescription = ReadString(item, "DocumentDescription") ?? string.Empty,
                DocumentIssuer = ReadString(item, "DocumentIssuer") ?? string.Empty,
                DocumentUploadDate = ReadString(item, "DocumentUploadDate") ?? string.Empty,
                Formats = ParseFormats(item)
            });
        }

        return list;
    }

    // CeG sends DocFormats as a JSON-encoded array of mime types (sometimes with embedded \r\n); unknown mimes are dropped.
    private static List<string> ParseFormats(JsonElement item)
    {
        var raw = item.TryGetProperty("DocFormats", out var el)
            ? (el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText())
            : null;
        var formats = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return formats;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return formats;
            foreach (var m in doc.RootElement.EnumerateArray())
            {
                var f = m.GetString() switch { "application/pdf" => "pdf", "application/xml" => "xml", "application/json" => "json", _ => null };
                if (f is not null) formats.Add(f);
            }
        }
        catch (JsonException) { }
        return formats;
    }

    public async Task<DigiLockerDocumentContent> GetDocumentAsync(string digiLockerId, string docUri, int fileType, CancellationToken ct)
    {
        EnsureConfigured();
        var ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        // Flat body, FileType as a string, TimeStamp as IST ddMMyyyyHHmmss -- the only shape the gateway accepts (confirmed against production).
        var payload = new Dictionary<string, string>
        {
            ["DeptKey"] = _options.DeptKey,
            ["DigilockerID"] = digiLockerId,
            ["DocURI"] = docUri,
            ["FileType"] = fileType.ToString(),
            ["APIMac"] = ApiMac(_options.DeptKey, digiLockerId, docUri),
            ["TimeStamp"] = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ist).ToString("ddMMyyyyHHmmss")
        };

        var resp = ParseRespObject(await PostAsync("GetDocumentFromDigiLocker", payload, ct));
        var b64 = ReadString(resp, "DocBytes");
        if (string.IsNullOrWhiteSpace(b64))
        {
            throw new InvalidOperationException("DigiLocker did not return a document.");
        }

        return new DigiLockerDocumentContent
        {
            Name = ReadString(resp, "DocName") ?? "document",
            MimeType = ReadString(resp, "DocType") ?? string.Empty,
            Bytes = Convert.FromBase64String(b64)
        };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.DeptKey) || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new DomainException("DigiLocker Gateway is not configured. Set DigiLocker:DeptKey (and HmacKey if different) before calling CeG.");
        }
    }

    private string ApiMac(params string[] parts)
    {
        var keyMaterial = string.IsNullOrWhiteSpace(_options.HmacKey) ? _options.DeptKey : _options.HmacKey;
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(keyMaterial);
        }
        catch (FormatException)
        {
            keyBytes = Encoding.UTF8.GetBytes(keyMaterial);
        }

        var message = string.Concat(parts);
        var hash = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private async Task<CegEnvelope> PostAsync(string method, Dictionary<string, string> payload, CancellationToken ct)
    {
        var url = _options.BaseUrl.TrimEnd('/') + "/" + method.TrimStart('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, RequestJson), Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"CeG {method} failed ({(int)response.StatusCode}).");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var statusCode = ReadStatusCode(root);
        var statusStr = ReadString(root, "StatusStr") ?? string.Empty;
        string? respObj = null;
        if (root.TryGetProperty("RespObj", out var respEl))
        {
            respObj = respEl.ValueKind == JsonValueKind.String ? respEl.GetString() : respEl.GetRawText();
        }

        if (statusCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(statusStr) ? $"CeG {method} returned StatusCode {statusCode}." : statusStr);
        }

        return new CegEnvelope(statusCode, statusStr, respObj);
    }

    private static JsonElement ParseRespObject(CegEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.RespObj))
        {
            throw new InvalidOperationException("CeG response RespObj was empty.");
        }

        using var doc = JsonDocument.Parse(envelope.RespObj);
        if (doc.RootElement.ValueKind == JsonValueKind.Object
            && (doc.RootElement.TryGetProperty("error", out _) || doc.RootElement.TryGetProperty("error_desc", out _)))
        {
            var desc = ReadString(doc.RootElement, "error_desc") ?? ReadString(doc.RootElement, "error") ?? "DigiLocker error.";
            throw new InvalidOperationException(desc);
        }

        return doc.RootElement.Clone();
    }

    private DigiLockerAadhaarDetails? ParseAadhaarFromResp(JsonElement obj, string digiLockerId)
    {
        foreach (var name in new[] { "DocBytes", "DataContent", "xml", "Xml", "eAadhaarXml", "AadhaarXml" })
        {
            var raw = ReadString(obj, name);
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var xml = raw.TrimStart().StartsWith('<') ? raw : TryDecodeBase64Utf8(raw);
            if (xml is not null && xml.TrimStart().StartsWith('<'))
            {
                var parsed = ParseAadhaarXml(xml, digiLockerId);
                if (parsed is not null) return parsed;
            }
        }

        var uid = FirstNonEmpty(
            ReadString(obj, "uid"),
            ReadString(obj, "UID"),
            ReadString(obj, "AadhaarNo"),
            ReadString(obj, "aadhaar"),
            ReadString(obj, "Aadhaar"))?.Trim();
        // CeG masks the real eAadhaar uid (8 placeholder chars + the real last 4 digits) rather
        // than sending the full unmasked 12-digit number -- accept that shape too (don't strip
        // non-digits first, that would destroy the masking placeholders and always fail the
        // length check), since AadhaarMasked only ever needs the last 4 real digits either way.
        if (uid is null || uid.Length != 12 || !uid[^4..].All(char.IsDigit))
        {
            return null;
        }

        var address = FirstNonEmpty(
            ReadString(obj, "Address"),
            ReadString(obj, "address"),
            JoinAddress(obj));

        return new DigiLockerAadhaarDetails
        {
            AadhaarMasked = $"XXXX-XXXX-{uid[^4..]}",
            AadhaarHash = Convert.ToHexString(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.AadhaarHashSalt), Encoding.UTF8.GetBytes(uid))).ToLowerInvariant(),
            Address = address,
            District = FirstNonEmpty(ReadString(obj, "dist"), ReadString(obj, "District"), ReadString(obj, "district")),
            Taluk = FirstNonEmpty(ReadString(obj, "subdist"), ReadString(obj, "Taluk"), ReadString(obj, "taluk")),
            StateName = FirstNonEmpty(ReadString(obj, "state"), ReadString(obj, "State"), ReadString(obj, "stateName")),
            VillageName = FirstNonEmpty(ReadString(obj, "vtc"), ReadString(obj, "Village"), ReadString(obj, "villageName")),
            Name = FirstNonEmpty(ReadString(obj, "name"), ReadString(obj, "Name")),
            DateOfBirth = ParseDob(FirstNonEmpty(ReadString(obj, "dob"), ReadString(obj, "DOB"), ReadString(obj, "date_of_birth"))),
            Gender = NormalizeGender(FirstNonEmpty(ReadString(obj, "gender"), ReadString(obj, "Gender")))
        };
    }

    private static string? JoinAddress(JsonElement obj)
    {
        var parts = new[] { "house", "street", "lm", "loc", "po" }
            .Select(n => ReadString(obj, n))
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private DigiLockerAadhaarDetails? ParseAadhaarXml(string xml, string sessionId)
    {
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        var doc = XDocument.Load(xmlReader);
        var uidElement = doc.Descendants().FirstOrDefault(e => e.Attribute("uid") is not null);
        var uid = uidElement?.Attribute("uid")?.Value?.Trim();
        // Same masked-uid shape as the non-XML fallback in ParseAadhaarFromResp -- CeG sends
        // 8 placeholder characters plus the real last 4 digits, not a full unmasked number.
        if (string.IsNullOrWhiteSpace(uid) || uid.Length != 12 || !uid[^4..].All(char.IsDigit))
        {
            _log.LogWarning("CeG eAadhaar XML for {DigiLockerId} had no recognizable 12-character uid", sessionId);
            return null;
        }

        var poa = doc.Descendants().FirstOrDefault(e => e.Attribute("dist") is not null || e.Attribute("state") is not null);
        string? Attr(string name) => poa?.Attribute(name)?.Value?.Trim() is { Length: > 0 } v ? v : null;

        var addressParts = new[] { Attr("house"), Attr("street"), Attr("lm"), Attr("loc"), Attr("po") }
            .Where(p => p is not null);
        var address = addressParts.Any() ? string.Join(", ", addressParts) : null;

        // Poi is a sibling of Poa under UidData -- the verified name/dob/gender, straight from
        // the signed KYC record. Stronger source than the basic profile call (GetDigilockerIDFromTxnID),
        // which often comes back with these fields empty.
        var poi = doc.Descendants("Poi").FirstOrDefault();
        var name = poi?.Attribute("name")?.Value?.Trim() is { Length: > 0 } n ? n : null;

        // LData is a third sibling under UidData -- the same name/address in the citizen's local
        // language (lang="07" = Kannada, for Karnataka's own DigiLocker gateway). Its attribute
        // set is narrower than Poa's (no separate house/street/lm/po), just co/loc/vtc.
        // Mirrors the English side's split exactly: Address is just the free-text portion
        // (here co/loc), District/StateName/VillageName are separate columns, not folded in.
        var lData = doc.Descendants("LData").FirstOrDefault();
        var nameKannada = lData?.Attribute("name")?.Value?.Trim() is { Length: > 0 } nk ? nk : null;
        string? LAttr(string attrName) => lData?.Attribute(attrName)?.Value?.Trim() is { Length: > 0 } v ? v : null;
        var addressKannadaParts = new[] { LAttr("co"), LAttr("loc") }.Where(p => p is not null);
        var addressKannada = addressKannadaParts.Any() ? string.Join(", ", addressKannadaParts) : null;
        var districtKannada = LAttr("dist");
        var stateKannada = LAttr("state");
        var villageKannada = LAttr("vtc");

        // Pht is a text-content sibling of Poi/Poa/LData under UidData in the UIDAI eKYC XML
        // spec (base64 JPEG), not an attribute. Soft-fail like everything else in this method --
        // a photo that doesn't decode shouldn't block the rest of a successful Aadhaar fetch.
        byte[]? photo = null;
        var phtText = doc.Descendants("Pht").FirstOrDefault()?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(phtText))
        {
            try
            {
                photo = Convert.FromBase64String(phtText);
            }
            catch (FormatException)
            {
                _log.LogWarning("CeG eAadhaar XML for {DigiLockerId} had a Pht element that wasn't valid base64", sessionId);
            }
        }

        return new DigiLockerAadhaarDetails
        {
            AadhaarMasked = $"XXXX-XXXX-{uid[^4..]}",
            AadhaarHash = Convert.ToHexString(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.AadhaarHashSalt), Encoding.UTF8.GetBytes(uid))).ToLowerInvariant(),
            Address = address,
            District = Attr("dist"),
            Taluk = Attr("subdist"),
            StateName = Attr("state"),
            VillageName = Attr("vtc"),
            Name = name,
            NameKannada = nameKannada,
            AddressKannada = addressKannada,
            DistrictKannada = districtKannada,
            StateKannada = stateKannada,
            VillageKannada = villageKannada,
            DateOfBirth = ParseDob(poi?.Attribute("dob")?.Value),
            Gender = NormalizeGender(poi?.Attribute("gender")?.Value),
            Photo = photo
        };
    }

    private static int ReadStatusCode(JsonElement root)
    {
        if (!root.TryGetProperty("StatusCode", out var el)) return -1;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
        return -1;
    }

    private static DateOnly? ParseDob(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        foreach (var format in DobFormats)
        {
            if (DateOnly.TryParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    // Must match portal.sevaone_user's ck_sevaone_user_gender check constraint exactly --
    // Male/Female/Other/PreferNotToSay, not single-letter codes. Anything unrecognized maps to
    // null rather than a raw passthrough, which could still violate the constraint.
    private static string? NormalizeGender(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Trim().ToUpperInvariant() switch
        {
            "M" or "MALE" => "Male",
            "F" or "FEMALE" => "Female",
            "T" or "O" or "OTHER" or "TRANSGENDER" => "Other",
            _ => null
        };
    }

    private static string? TryDecodeBase64Utf8(string raw)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(raw));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private readonly record struct CegEnvelope(int StatusCode, string StatusStr, string? RespObj);
}
