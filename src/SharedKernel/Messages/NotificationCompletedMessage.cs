namespace SharedKernel
{

    public sealed class NotificationCompletedMessage
    {
        public Guid NotificationId { get; init; }
        public string MessageId { get; init; } = null!;
        public string SourceId { get; init; } = null!;
        public string EventType { get; init; } = null!;
        public NotificationChannel Channel { get; init; }
        public string Recipient { get; init; } = null!;
        public string? Subject { get; init; }
        public string? Body { get; init; }
        public string? Payload { get; init; }
        public NotificationStatus Status { get; init; }
        public string? Provider { get; init; }
        public string? FailureReason { get; init; }
        public DateTimeOffset CompletedAt { get; init; }
    }
}
