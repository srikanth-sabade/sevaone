using System.Globalization;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Models;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// What an admin can put in a service's Key1..Key5 box: plain text (sent as is), "{citizen:field}" (taken from the signed-in
/// citizen's record) or "{ask:Question}" (the citizen is asked when they press Apply). Stored as the same text, so older
/// plain values keep working.
/// </summary>
public static class EdcsKeyValue
{
    public static readonly IReadOnlyDictionary<string, Func<Citizen, string?>> CitizenFields = new Dictionary<string, Func<Citizen, string?>>(StringComparer.Ordinal)
    {
        ["name"] = c => c.Name, ["mobile"] = c => c.Mobile, ["email"] = c => c.Email,
        ["aadhaarHash"] = c => c.AadhaarHash, ["aadhaarMasked"] = c => c.AadhaarMasked,
        ["dateOfBirth"] = c => c.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ["gender"] = c => c.Gender,
        ["address"] = c => c.Address, ["district"] = c => c.District, ["taluk"] = c => c.Taluk,
        ["state"] = c => c.StateName, ["village"] = c => c.VillageName, ["digilockerId"] = c => c.DigiLockerId,
        ["sevaId"] = c => c.SevaId, ["username"] = c => c.Username, ["sevaOneUserId"] = c => c.SevaOneUserId.ToString(CultureInfo.InvariantCulture),
    };

    public static bool IsCitizenField(string? raw, out string field) => Wrapped(raw, "{citizen:", out field);

    public static bool IsAsk(string? raw, out string label) => Wrapped(raw, "{ask:", out label);

    /// <summary>The value to send for this key, or null when there is nothing to send (blank, unknown field, empty record field
    /// or a question that has not been answered).</summary>
    public static string? Resolve(string? raw, Citizen citizen)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (IsAsk(raw, out _)) return null;
        if (IsCitizenField(raw, out var field))
            return CitizenFields.TryGetValue(field, out var read) && read(citizen) is { Length: > 0 } v ? v : null;
        return raw;
    }

    /// <summary>The five key settings an admin saved for a service. A key left blank is not sent.</summary>
    public static string?[] RawKeys(string? extraConfig)
    {
        var k = TokenStatusJson.ReadEdcsKeys(extraConfig);
        return new[] { k.Key1, k.Key2, k.Key3, k.Key4, k.Key5 };
    }

    /// <summary>Works out Key1..Key5 for one citizen. Questions the citizen has answered are used; a question still unanswered
    /// comes back in Needs so the caller can ask before any token is made.</summary>
    public static (Dictionary<string, string> Values, List<ApplyInputRequestDto> Needs) Build(
        IReadOnlyList<string?> raw, Citizen citizen, IReadOnlyDictionary<string, string>? answers)
    {
        var values = new Dictionary<string, string>();
        var needs = new List<ApplyInputRequestDto>();
        for (var i = 0; i < raw.Count; i++)
        {
            var key = $"Key{i + 1}";
            if (IsAsk(raw[i], out var label))
            {
                var answer = answers is not null && answers.TryGetValue(key, out var a) ? Clean(a) : null;
                if (answer is null) needs.Add(new ApplyInputRequestDto(key, label)); else values[key] = answer;
            }
            else if (Resolve(raw[i], citizen) is { } value) values[key] = value;
        }
        return (values, needs);
    }

    private static string? Clean(string? answer)
    {
        var t = new string((answer ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
        return t.Length == 0 ? null : t.Length > 200 ? t[..200] : t;
    }

    /// <summary>An error for the admin when the text looks like one of the "{...}" forms but is not valid; null when fine.</summary>
    public static string? Validate(string? raw, string keyName)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.Length > 300) return $"{keyName} is too long.";
        if (IsCitizenField(raw, out var field) && !CitizenFields.ContainsKey(field)) return $"{keyName}: '{field}' is not a field we hold for the citizen.";
        if (IsAsk(raw, out var label) && string.IsNullOrWhiteSpace(label)) return $"{keyName}: write the question to ask the citizen.";
        return null;
    }

    private static bool Wrapped(string? raw, string prefix, out string inner)
    {
        inner = "";
        var t = raw?.Trim();
        if (t is null || !t.StartsWith(prefix, StringComparison.Ordinal) || !t.EndsWith('}')) return false;
        inner = t[prefix.Length..^1].Trim();
        return true;
    }
}
