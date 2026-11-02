using SevaOne.Business.Discovery.Interfaces;

namespace SevaOne.Services.BackgroundServices;

/// <summary>Keeps token-integration applications up to date without the citizen opening the page: every few minutes it
/// asks the department's status API about the applications that are due (acknowledged, not finished, not asked
/// recently). Slow or failing departments cannot hold it up: each call has its own short timeout.</summary>
public sealed class TokenIntegrationStatusPoller : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<TokenIntegrationStatusPoller> _logger;

    public TokenIntegrationStatusPoller(IServiceScopeFactory scopes, ILogger<TokenIntegrationStatusPoller> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var asked = await scope.ServiceProvider.GetRequiredService<ITokenStatusService>().PollDueAsync(limit: 20, stoppingToken);
                    if (asked > 0) _logger.LogInformation("Token integration status check asked {Count} application(s).", asked);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Token integration status poll failed.");
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
