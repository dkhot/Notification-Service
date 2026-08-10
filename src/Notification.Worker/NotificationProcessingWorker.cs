using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using SharedKernel;

namespace Notification.Worker
{
    internal sealed class NotificationProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationProcessingWorker> _logger;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

        public NotificationProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                    var handler = scope.ServiceProvider.GetRequiredService<IProcessNotificationHandler>();

                    var message = await queue.DequeueAsync<NotificationRequestMessage>(stoppingToken);
                    if (message == null)
                    {
                        await Task.Delay(_delay, stoppingToken);
                        continue;
                    }

                    await handler.HandleAsync(message.Payload, stoppingToken);
                    await queue.MarkProcessedAsync(message.Id, true, null, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Unexpected error while processing notifications.");
                    await Task.Delay(_delay, stoppingToken);
                }
            }
        }
    }
}
