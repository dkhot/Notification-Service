using Notification.Application.Abstractions;
using Notification.Application.Callbacks;
using SharedKernel;

namespace Callback.Dispatcher
{
    internal sealed class CallbackDispatcherWorker : BackgroundService
    {
        private readonly IMessageQueue _queue;
        private readonly IHandleNotificationCompletedHandler _completionHandler;
        private readonly IDispatchPendingCallbackHandler _dispatchHandler;
        private readonly ILogger<CallbackDispatcherWorker> _logger;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

        public CallbackDispatcherWorker(
            IMessageQueue queue,
            IHandleNotificationCompletedHandler completionHandler,
            IDispatchPendingCallbackHandler dispatchHandler,
            ILogger<CallbackDispatcherWorker> logger)
        {
            _queue = queue;
            _completionHandler = completionHandler;
            _dispatchHandler = dispatchHandler;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Callback dispatcher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queue.DequeueAsync<NotificationCompletedMessage>(stoppingToken);
                    if (message != null)
                    {
                        await _completionHandler.HandleAsync(message.Payload, stoppingToken);
                        await _queue.MarkProcessedAsync(message.Id, true, null, stoppingToken);
                    }

                    await _dispatchHandler.DispatchNextAsync(stoppingToken);
                    await Task.Delay(_delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Unexpected error while dispatching callbacks.");
                    await Task.Delay(_delay, stoppingToken);
                }
            }
        }
    }
}
