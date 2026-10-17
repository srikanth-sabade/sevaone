namespace SevaOne.Infrastructure.AiBackend;

/// <summary>Server-to-server connection details for the optional GPU-host telemetry sidecar.
/// An empty BaseUrl keeps the existing AI status behavior when the sidecar is not deployed.</summary>
public sealed class AiMonitorOptions
{
    public const string SectionName = "AiMonitor";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiKeyFile { get; set; } = string.Empty;
}
