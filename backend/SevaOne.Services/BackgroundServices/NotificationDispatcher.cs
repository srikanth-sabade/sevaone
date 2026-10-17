using SevaOne.Infrastructure.Repositories.Notification;

namespace SevaOne.Services.BackgroundServices;

/// <summary>Drains notification.notifications and "sends" each pending row — logging is the whole channel for this PoC (no real SMS/email gateway wired up), matching the original NotificationWorker.</summary>
public sealed class NotificationDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(IServiceScopeFactory scopes, ILogger<NotificationDispatcher> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var pending = await repository.GetPendingAsync(batchSize: 50, stoppingToken);

                foreach (var note in pending)
                {
                    _logger.LogInformation(
                        "Notify [{Channel}] to {Recipient}: {Subject} — {Body}",
                        note.Channel,
                        string.IsNullOrWhiteSpace(note.Recipient) ? "(unknown)" : note.Recipient,
                        note.Subject,
                        note.Body);
                    await repository.MarkSentAsync(note.Id, DateTimeOffset.UtcNow, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Notification dispatcher cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        }
    }
}
