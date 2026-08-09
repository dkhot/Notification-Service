using Microsoft.Extensions.Logging;
using Notification.Application.Abstractions;
using Notification.Domain.Repositories;
using SharedKernel;

namespace Notification.Application.Notifications
{
    public sealed class ProcessNotificationHandler : IProcessNotificationHandler
    {
        private readonly INotificationRepository _notifications;
        private readonly ISourceConfigurationRepository _sourceConfigurations;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationProviderResolver _providerResolver;
        private readonly IMessageQueue _messageQueue;
        private readonly ILogger<ProcessNotificationHandler> _logger;

        public ProcessNotificationHandler(
            INotificationRepository notifications,
            ISourceConfigurationRepository sourceConfigurations,
            IUnitOfWork unitOfWork,
            INotificationProviderResolver providerResolver,
            IMessageQueue messageQueue,
            ILogger<ProcessNotificationHandler> logger)
        {
            _notifications = notifications;
            _sourceConfigurations = sourceConfigurations;
            _unitOfWork = unitOfWork;
            _providerResolver = providerResolver;
            _messageQueue = messageQueue;
            _logger = logger;
        }

        public async Task HandleAsync(NotificationRequestMessage request, CancellationToken cancellationToken = default)
        {
            var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
            {
                _logger.LogWarning("Received queue message for missing notification {NotificationId}.", request.NotificationId);
                return;
            }

            notification.MarkProcessing();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                var provider = _providerResolver.Resolve(request.Channel);
                await provider.SendAsync(request, cancellationToken);

                var sourceConfig = await _sourceConfigurations.GetBySourceIdAsync(request.SourceId, cancellationToken);
                var hasCallback = sourceConfig != null && sourceConfig.Enabled && !string.IsNullOrWhiteSpace(sourceConfig.WebhookUrl);

                notification.MarkSent(provider.Name, hasCallback);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var completedMessage = new NotificationCompletedMessage
                {
                    NotificationId = notification.Id,
                    MessageId = notification.MessageId,
                    SourceId = notification.SourceId,
                    EventType = notification.EventType,
                    Channel = notification.Channel,
                    Recipient = notification.Recipient,
                    Subject = notification.Subject,
                    Body = notification.Body,
                    Payload = notification.Payload,
                    Status = notification.Status,
                    Provider = notification.Provider,
                    FailureReason = notification.FailureReason,
                    CompletedAt = notification.CompletedAt.GetValueOrDefault(DateTimeOffset.UtcNow)
                };

                await _messageQueue.EnqueueAsync(completedMessage, cancellationToken);
            }
            catch (Exception failure)
            {
                notification.MarkFailed(failure.Message);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogError(failure, "Failed to dispatch notification {NotificationId}.", request.NotificationId);
            }
        }
    }
}
