using System.Text.Json.Serialization;

namespace SevaOne.Infrastructure.AiBackend;

public interface IAiMonitorClient
{
    Task<AiMonitorMetricsWireResponse?> GetMetricsAsync(CancellationToken ct);
}

public sealed record AiMonitorMetricsWireResponse(
    DateTimeOffset CollectedAt,
    string Host,
    AiMonitorCpuWire Cpu,
    AiMonitorMemoryWire Memory,
    AiMonitorDiskWire Disk,
    IReadOnlyList<AiMonitorGpuWire> Gpus);

public sealed record AiMonitorCpuWire(
    double UsagePercent,
    double? Load1,
    int Cores);

public sealed record AiMonitorMemoryWire(
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes,
    double UsagePercent);

public sealed record AiMonitorDiskWire(
    string Mount,
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes,
    double UsagePercent);

public sealed record AiMonitorGpuWire(
    string Name,
    double UtilizationPercent,
    long MemoryTotalBytes,
    long MemoryUsedBytes,
    double MemoryUsagePercent,
    [property: JsonPropertyName("temperatureCelsius")] double? TemperatureCelsius);
