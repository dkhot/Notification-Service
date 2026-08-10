using Notification.Application.Abstractions;
using Notification.Application.Callbacks;
using SharedKernel;

namespace Callback.Dispatcher
{
    internal sealed class CallbackDispatcherWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CallbackDispatcherWorker> _logger;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

        public CallbackDispatcherWorker(IServiceScopeFactory scopeFactory, ILogger<CallbackDispatcherWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Callback dispatcher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                    var completionHandler = scope.ServiceProvider.GetRequiredService<IHandleNotificationCompletedHandler>();
                    var dispatchHandler = scope.ServiceProvider.GetRequiredService<IDispatchPendingCallbackHandler>();

                    var message = await queue.DequeueAsync<NotificationCompletedMessage>(stoppingToken);
                    if (message != null)
                    {
                        await completionHandler.HandleAsync(message.Payload, stoppingToken);
                        await queue.MarkProcessedAsync(message.Id, true, null, stoppingToken);
                    }

                    await dispatchHandler.DispatchNextAsync(stoppingToken);
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
