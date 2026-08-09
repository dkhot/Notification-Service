namespace Notification.Application.Notifications.Validators
{
    public static class CreateNotificationCommandValidator
    {
        public static void Validate(CreateNotificationCommand request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.MessageId))
            {
                throw new ArgumentException("MessageId is required.", nameof(request.MessageId));
            }

            if (string.IsNullOrWhiteSpace(request.SourceId))
            {
                throw new ArgumentException("SourceId is required.", nameof(request.SourceId));
            }

            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                throw new ArgumentException("EventType is required.", nameof(request.EventType));
            }

            if (string.IsNullOrWhiteSpace(request.Recipient))
            {
                throw new ArgumentException("Recipient is required.", nameof(request.Recipient));
            }
        }
    }
}
