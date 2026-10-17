using System.Text.Json;
using SevaOne.Business.Common;
using SevaOne.Business.Orchestration.Interfaces;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Connectors;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Services.BackgroundServices;

/// <summary>Picks up department-side status changes SevaOne wasn't pushed for (no webhook wired for every department) by polling GetStatusAsync on each in-flight application's connector.</summary>
public sealed class ApplicationStatusPoller : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ApplicationStatusPoller> _logger;

    public ApplicationStatusPoller(IServiceScopeFactory scopes, ILogger<ApplicationStatusPoller> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Status poller cycle failed.");
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on host shutdown -- the delay is cancelled along with everything else.
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var orchestration = scope.ServiceProvider.GetRequiredService<IOrchestrationRepository>();
        var status = scope.ServiceProvider.GetRequiredService<IStatusRepository>();
        var sessions = scope.ServiceProvider.GetRequiredService<IDepartmentSessionService>();
        var connectors = scope.ServiceProvider.GetRequiredService<IDepartmentConnectorRegistry>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var inflight = await orchestration.GetInflightApplicationsAsync(batchSize: 25, ct);

        foreach (var application in inflight)
        {
            try
            {
                var opened = await sessions.GetOrOpenAsync(application.CitizenId, application.DepartmentCode, ct);
                var remote = await connectors.Get(application.DepartmentCode).GetStatusAsync(opened.Handle, application.DepartmentReference, ct);
                var next = ApplicationStatusMapper.FromDepartment(remote.Status);
                if ((int)next == application.Status)
                {
                    continue;
                }

                await orchestration.UpdateApplicationStatusAsync(application.Id, next, ct);
                await status.InsertApplicationEventAsync(new ApplicationEvent
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = application.Id,
                    Status = next,
                    Note = string.IsNullOrWhiteSpace(remote.Note) ? "Polled from department" : remote.Note,
                    At = clock.UtcNow
                }, ct);
                await orchestration.InsertOutboxMessageAsync(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Topic = "application.status_changed",
                    Payload = JsonSerializer.Serialize(new { sevaReference = application.SevaReference, status = ApplicationStatusMapper.ToApi(next) }),
                    CreatedAt = clock.UtcNow
                }, uow: null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Poll failed for {Ref}", application.SevaReference);
            }
        }
    }
}
