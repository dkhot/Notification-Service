using SharedKernel;

namespace Notification.Domain.Entities
{
    public sealed class Notification
    {
        public Guid Id { get; private set; }
        public string MessageId { get; private set; } = null!;
        public string SourceId { get; private set; } = null!;
        public string EventType { get; private set; } = null!;
        public NotificationChannel Channel { get; private set; }
        public string Recipient { get; private set; } = null!;
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public string? Payload { get; private set; }
        public NotificationStatus Status { get; private set; }
        public string? Provider { get; private set; }
        public string? FailureReason { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? CompletedAt { get; private set; }

        private Notification()
        {
        }

        public static Notification Create(
            string messageId,
            string sourceId,
            string eventType,
            NotificationChannel channel,
            string recipient,
            string? subject,
            string? body,
            string? payload)
        {
            return new Notification
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                SourceId = sourceId,
                EventType = eventType,
                Channel = channel,
                Recipient = recipient,
                Subject = subject,
                Body = body,
                Payload = payload,
                Status = NotificationStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        public void MarkProcessing()
        {
            Status = NotificationStatus.Processing;
        }

        public void MarkSent(string provider, bool hasCallback)
        {
            Provider = provider;
            FailureReason = null;
            CompletedAt = DateTimeOffset.UtcNow;
            Status = hasCallback ? NotificationStatus.CallbackPending : NotificationStatus.Sent;
        }

        public void MarkFailed(string failureReason)
        {
            Status = NotificationStatus.Failed;
            FailureReason = failureReason;
            CompletedAt = DateTimeOffset.UtcNow;
        }

        public void MarkCallbackPending()
        {
            Status = NotificationStatus.CallbackPending;
        }

        public void MarkCallbackCompleted()
        {
            Status = NotificationStatus.CallbackCompleted;
        }

        public void MarkCallbackFailed()
        {
            Status = NotificationStatus.CallbackFailed;
        }
    }
}
