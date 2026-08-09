using Microsoft.Extensions.Logging;
using Notification.Domain.Repositories;
using SharedKernel;
using CallbackEntity = Notification.Domain.Entities.Callback;

namespace Notification.Application.Callbacks
{
    public sealed class HandleNotificationCompletedHandler : IHandleNotificationCompletedHandler
    {
        private readonly INotificationRepository _notifications;
        private readonly ICallbackRepository _callbacks;
        private readonly ISourceConfigurationRepository _sourceConfigurations;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HandleNotificationCompletedHandler> _logger;

        public HandleNotificationCompletedHandler(
            INotificationRepository notifications,
            ICallbackRepository callbacks,
            ISourceConfigurationRepository sourceConfigurations,
            IUnitOfWork unitOfWork,
            ILogger<HandleNotificationCompletedHandler> logger)
        {
            _notifications = notifications;
            _callbacks = callbacks;
            _sourceConfigurations = sourceConfigurations;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleAsync(NotificationCompletedMessage message, CancellationToken cancellationToken = default)
        {
            var sourceConfig = await _sourceConfigurations.GetBySourceIdAsync(message.SourceId, cancellationToken);
            if (sourceConfig == null || !sourceConfig.Enabled || string.IsNullOrWhiteSpace(sourceConfig.WebhookUrl))
            {
                _logger.LogInformation("Skipping callback for source {SourceId} because source configuration is missing or disabled.", message.SourceId);
                return;
            }

            var callback = CallbackEntity.Create(message.NotificationId, message.SourceId, sourceConfig.WebhookUrl);
            await _callbacks.AddAsync(callback, cancellationToken);

            var notification = await _notifications.GetByIdAsync(message.NotificationId, cancellationToken);
            notification?.MarkCallbackPending();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
