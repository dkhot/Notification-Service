namespace Notification.Infrastructure.Persistence
{
    public sealed class MessageQueueItem
    {
        public Guid Id { get; set; }
        public string MessageType { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
        public DateTimeOffset? ProcessedAt { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int AttemptCount { get; set; }
    }
}
