using System.Text.Json;

namespace SevaOne.Business.Common;

public static class JsonLists
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<string> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    public static string Write(IEnumerable<string>? values) =>
        JsonSerializer.Serialize(values?.ToArray() ?? [], Options);
}
