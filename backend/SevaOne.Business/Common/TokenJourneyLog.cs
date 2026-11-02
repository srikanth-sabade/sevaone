using Microsoft.Extensions.Logging;

namespace SevaOne.Business.Common;

/// <summary>
/// Structured console-friendly token journey logs.
/// Stages: T1 WSO2 · T2 OTP · T3 seva.sid · T4 dept handle · T6 SevaOne JWS · MACHINE dept client.
/// Never log full secrets / raw JWT / OTP plaintext.
/// </summary>
public static class TokenJourneyLog
{
    public static void Emit(
        ILogger log,
        string stage,
        string action,
        string detail,
        params (string Key, string? Value)[] fields)
    {
        var parts = new List<string> { $"[TOKEN:{stage}:{action}] {detail}" };
        foreach (var (key, value) in fields)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}={value}");
            }
        }

        log.LogInformation("{TokenJourney}", string.Join(" · ", parts));
    }
}
