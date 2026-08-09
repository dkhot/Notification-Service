namespace Notification.Domain.Exceptions
{
    public sealed class DuplicateNotificationException : Exception
    {
        public string MessageId { get; }

        public DuplicateNotificationException(string messageId)
            : base($"Notification with MessageId '{messageId}' already exists.")
        {
            MessageId = messageId;
        }
    }
}
