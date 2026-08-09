using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using SharedKernel;

namespace Notification.Worker
{
    internal sealed class NotificationProcessingWorker : BackgroundService
    {
        private readonly IMessageQueue _queue;
        private readonly IProcessNotificationHandler _handler;
        private readonly ILogger<NotificationProcessingWorker> _logger;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

        public NotificationProcessingWorker(IMessageQueue queue, IProcessNotificationHandler handler, ILogger<NotificationProcessingWorker> logger)
        {
            _queue = queue;
            _handler = handler;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queue.DequeueAsync<NotificationRequestMessage>(stoppingToken);
                    if (message == null)
                    {
                        await Task.Delay(_delay, stoppingToken);
                        continue;
                    }

                    await _handler.HandleAsync(message.Payload, stoppingToken);
                    await _queue.MarkProcessedAsync(message.Id, true, null, stoppingToken);
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
