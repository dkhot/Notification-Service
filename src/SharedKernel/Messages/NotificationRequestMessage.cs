namespace SharedKernel
{
    public sealed class NotificationRequestMessage
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
        public DateTimeOffset CreatedAt { get; init; }
    }
}
