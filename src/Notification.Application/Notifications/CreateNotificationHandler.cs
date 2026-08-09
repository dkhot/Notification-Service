using Notification.Application.Abstractions;
using Notification.Application.Notifications.Validators;
using Notification.Domain.Exceptions;
using Notification.Domain.Repositories;
using SharedKernel;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Application.Notifications
{
    public sealed class CreateNotificationHandler : ICreateNotificationHandler
    {
        private readonly INotificationRepository _notifications;
        private readonly ISourceConfigurationRepository _sourceConfigurations;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageQueue _messageQueue;

        public CreateNotificationHandler(
            INotificationRepository notifications,
            ISourceConfigurationRepository sourceConfigurations,
            IUnitOfWork unitOfWork,
            IMessageQueue messageQueue)
        {
            _notifications = notifications;
            _sourceConfigurations = sourceConfigurations;
            _unitOfWork = unitOfWork;
            _messageQueue = messageQueue;
        }

        public async Task<Guid> HandleAsync(CreateNotificationCommand request, CancellationToken cancellationToken = default)
        {
            CreateNotificationCommandValidator.Validate(request);

            var sourceConfig = await _sourceConfigurations.GetBySourceIdAsync(request.SourceId, cancellationToken);
            if (sourceConfig == null || !sourceConfig.Enabled)
            {
                throw new SourceNotRegisteredException(request.SourceId);
            }

            var exists = await _notifications.ExistsByMessageIdAsync(request.MessageId, cancellationToken);
            if (exists)
            {
                throw new DuplicateNotificationException(request.MessageId);
            }

            var notification = NotificationEntity.Create(
                request.MessageId,
                request.SourceId,
                request.EventType,
                request.Channel,
                request.Recipient,
                request.Subject,
                request.Body,
                request.Payload);

            await _notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var queueMessage = new NotificationRequestMessage
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
                CreatedAt = notification.CreatedAt
            };

            await _messageQueue.EnqueueAsync(queueMessage, cancellationToken);
            return notification.Id;
        }
    }
}
