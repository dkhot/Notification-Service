using Microsoft.Extensions.Logging;
using Notification.Application.Abstractions;
using Notification.Domain.Repositories;

namespace Notification.Application.Callbacks
{
    public sealed class DispatchPendingCallbackHandler : IDispatchPendingCallbackHandler
    {
        private readonly ICallbackRepository _callbacks;
        private readonly INotificationRepository _notifications;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebhookSender _webhookSender;
        private readonly ILogger<DispatchPendingCallbackHandler> _logger;

        public DispatchPendingCallbackHandler(
            ICallbackRepository callbacks,
            INotificationRepository notifications,
            IUnitOfWork unitOfWork,
            IWebhookSender webhookSender,
            ILogger<DispatchPendingCallbackHandler> logger)
        {
            _callbacks = callbacks;
            _notifications = notifications;
            _unitOfWork = unitOfWork;
            _webhookSender = webhookSender;
            _logger = logger;
        }

        public async Task<bool> DispatchNextAsync(CancellationToken cancellationToken = default)
        {
            var callback = await _callbacks.GetNextDueAsync(cancellationToken);
            if (callback == null)
            {
                return false;
            }

            callback.MarkAttempt();
            var notification = await _notifications.GetByIdAsync(callback.NotificationId, cancellationToken);

            var payload = new CallbackWebhookPayload
            {
                NotificationId = callback.NotificationId,
                SourceId = callback.SourceId,
                WebhookUrl = callback.WebhookUrl,
                RetryCount = callback.RetryCount,
                Status = callback.Status
            };

            var result = await _webhookSender.SendAsync(callback.WebhookUrl, payload, cancellationToken);

            if (result.Success)
            {
                callback.MarkCompleted();
                notification?.MarkCallbackCompleted();
            }
            else
            {
                var exhausted = callback.ScheduleRetry(result.Error ?? "Webhook delivery failed.");
                if (exhausted)
                {
                    notification?.MarkCallbackFailed();
                    _logger.LogError("Callback for notification {NotificationId} exhausted retries: {Reason}", callback.NotificationId, callback.FailureReason);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
