using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SevaOne.Business.Common;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;

namespace SevaOne.Services.Controllers;

[Route("api/v1")]
public sealed class HealthController : SevaControllerBase
{
    /// <summary>Liveness — is the process itself up and responding. Never checks a dependency; that's Ready's job.</summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", service = "sevaone" });

    /// <summary>Readiness — can this instance actually serve traffic. Runs all registered health checks (Postgres, Redis, Kafka, WSO2) and reports per-dependency status; 503 if any is unhealthy.</summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready([FromServices] HealthCheckService healthChecks, CancellationToken ct)
    {
        var report = await healthChecks.CheckHealthAsync(ct);
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), description = e.Value.Description })
        };

        return report.Status == HealthStatus.Healthy ? Ok(body) : StatusCode(503, body);
    }

    /// <summary>Superadmin operational view with dependency status and safe process-level node
    /// telemetry. This deliberately omits exception details, connection strings, and citizen
    /// data; public liveness/readiness remain available through the routes above.</summary>
    [Authorize(AuthenticationSchemes = "SevaGovAdmin")]
    [HttpGet("admin/gov/management/health")]
    public async Task<IActionResult> ManagementHealth(
        [FromServices] HealthCheckService healthChecks,
        [FromServices] ICurrentGovAdmin current,
        CancellationToken ct)
    {
        if (current.Tier != GovAdminTier.Superadmin)
        {
            throw new ForbiddenException("Only a Superadmin can view system health.");
        }

        var report = await healthChecks.CheckHealthAsync(ct);
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var startedAt = process.StartTime.ToUniversalTime();
        var uptimeSeconds = Math.Max(0, (long)(DateTime.UtcNow - startedAt).TotalSeconds);
        var checks = report.Entries
            .Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1)
            })
            .Append(new
            {
                name = "bff",
                status = HealthStatus.Healthy.ToString(),
                description = (string?)"BFF process is serving requests.",
                durationMs = 0d
            })
            .OrderBy(entry => entry.name)
            .ToArray();

        return Ok(new
        {
            status = report.Status.ToString(),
            checkedAt = DateTimeOffset.UtcNow,
            checks,
            node = new
            {
                machineName = Environment.MachineName,
                runtime = Environment.Version.ToString(),
                processId = Environment.ProcessId,
                processorCount = Environment.ProcessorCount,
                uptimeSeconds,
                workingSetBytes = process.WorkingSet64,
                privateMemoryBytes = process.PrivateMemorySize64,
                managedHeapBytes = GC.GetTotalMemory(forceFullCollection: false)
            }
        });
    }
}
