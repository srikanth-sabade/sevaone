using SevaOne.Business.Discovery.Interfaces;

namespace SevaOne.Services.BackgroundServices;

/// <summary>Fetches acknowledgements that departments reported to the Seva Sindhu API ("edcs_api" token integration): every
/// couple of minutes it asks that API about the applications still waiting. Each call has its own short timeout, so a slow
/// Seva Sindhu cannot hold anything up.</summary>
public sealed class EdcsApiAckPoller : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EdcsApiAckPoller> _logger;

    public EdcsApiAckPoller(IServiceScopeFactory scopes, ILogger<EdcsApiAckPoller> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var done = await scope.ServiceProvider.GetRequiredService<IEdcsApiAckService>().PollDueAsync(limit: 20, stoppingToken);
                    if (done > 0) _logger.LogInformation("Seva Sindhu acknowledgement pull-back recorded {Count} application(s).", done);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Seva Sindhu acknowledgement pull-back failed.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
    }
}
