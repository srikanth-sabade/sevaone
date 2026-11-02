using System.Text.Json;
using System.Text.RegularExpressions;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Domain.Enums;

namespace SevaOne.Business.Discovery.Services;

/// <summary>The optional "Key1".."Key5" fields Seva Sindhu's generate API accepts. Left blank unless a department
/// says it needs one of these (for example an Aadhaar hash).</summary>
public sealed record EdcsKeys(string? Key1 = null, string? Key2 = null, string? Key3 = null, string? Key4 = null, string? Key5 = null)
{
    public bool HasAny => !string.IsNullOrWhiteSpace(Key1) || !string.IsNullOrWhiteSpace(Key2) || !string.IsNullOrWhiteSpace(Key3)
        || !string.IsNullOrWhiteSpace(Key4) || !string.IsNullOrWhiteSpace(Key5);
}

/// <summary>Reading a department's status reply and the saved configuration. Kept separate so it is easy to test.</summary>
public static partial class TokenStatusJson
{
    public static readonly string[] AuthTypes = ["none", "basic", "bearer", "header", "provider"];

    [GeneratedRegex(@"^([^\[\]\.]+)((?:\[\d+\])*)$")]
    private static partial Regex Segment();

    /// <summary>Reads one value by a simple path such as "status", "data.state" or "result[0].status".
    /// Names are matched ignoring case. Only text, numbers and true/false come back; objects, lists and null give null.</summary>
    public static string? ReadString(JsonElement root, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var current = root;
        var text = path.Trim();
        if (text.StartsWith("$.")) text = text[2..];

        foreach (var raw in text.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Segment().Match(raw);
            if (!match.Success) return null;

            if (current.ValueKind != JsonValueKind.Object) return null;
            JsonElement? next = null;
            foreach (var property in current.EnumerateObject())
            {
                if (string.Equals(property.Name, match.Groups[1].Value, StringComparison.OrdinalIgnoreCase)) { next = property.Value; break; }
            }
            if (next is null) return null;
            current = next.Value;

            foreach (Match index in Regex.Matches(match.Groups[2].Value, @"\[(\d+)\]"))
            {
                if (current.ValueKind != JsonValueKind.Array || !int.TryParse(index.Groups[1].Value, out var i) || i >= current.GetArrayLength()) return null;
                current = current[i];
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    /// <summary>The department's word to one of our statuses (case and surrounding spaces ignored); null when not mapped.</summary>
    public static ApplicationStatus? Map(TokenStatusConfig config, string? departmentStatus)
    {
        if (string.IsNullOrWhiteSpace(departmentStatus) || config.StatusMap is null) return null;
        foreach (var (key, value) in config.StatusMap)
        {
            if (string.Equals(key.Trim(), departmentStatus.Trim(), StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse<ApplicationStatus>(value, ignoreCase: true, out var status))
                return status;
        }
        return null;
    }

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Reads the status settings out of an integration's extra_config. Missing or damaged settings give the defaults.</summary>
    public static TokenStatusConfig Read(string? extraConfig)
    {
        if (string.IsNullOrWhiteSpace(extraConfig)) return new TokenStatusConfig();
        try
        {
            using var doc = JsonDocument.Parse(extraConfig);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("status", out var status))
                return JsonSerializer.Deserialize<TokenStatusConfig>(status.GetRawText(), Options) ?? new TokenStatusConfig();
        }
        catch (JsonException) { }
        return new TokenStatusConfig();
    }

    public static string Write(TokenStatusConfig config, long? kaveriServiceId = null, EdcsKeys? edcsKeys = null)
    {
        var root = new Dictionary<string, object> { ["status"] = config };
        if (kaveriServiceId is { } id) root["kaveri"] = new { edcsServiceId = id };
        if (edcsKeys is not null && edcsKeys.HasAny) root["edcsKeys"] = edcsKeys;
        return JsonSerializer.Serialize(root, Options);
    }

    /// <summary>The extra "Key1".."Key5" fields some departments need in Seva Sindhu's generate call (for example an
    /// Aadhaar hash). Most services leave all of these empty.</summary>
    public static EdcsKeys ReadEdcsKeys(string? extraConfig)
    {
        if (string.IsNullOrWhiteSpace(extraConfig)) return new EdcsKeys();
        try
        {
            using var doc = JsonDocument.Parse(extraConfig);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("edcsKeys", out var keys))
                return JsonSerializer.Deserialize<EdcsKeys>(keys.GetRawText(), Options) ?? new EdcsKeys();
        }
        catch (JsonException) { }
        return new EdcsKeys();
    }

    /// <summary>The Kaveri service id a SevaOne service stands for, from its extra_config; null when not set.</summary>
    public static long? ReadKaveriServiceId(string? extraConfig)
    {
        if (string.IsNullOrWhiteSpace(extraConfig)) return null;
        try
        {
            using var doc = JsonDocument.Parse(extraConfig);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("kaveri", out var kaveri)
                && kaveri.ValueKind == JsonValueKind.Object && kaveri.TryGetProperty("edcsServiceId", out var id) && id.TryGetInt64(out var value) && value > 0)
                return value;
        }
        catch (JsonException) { }
        return null;
    }
}
