namespace Notification.Application.Abstractions
{
    public interface IMessageQueue
    {
        Task EnqueueAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
        Task<QueueMessage<T>?> DequeueAsync<T>(CancellationToken cancellationToken = default) where T : class;
        Task MarkProcessedAsync(Guid messageId, bool success, string? error = null, CancellationToken cancellationToken = default);
    }

    public sealed class QueueMessage<T> where T : class
    {
        public Guid Id { get; init; }
        public T Payload { get; init; } = null!;
        public DateTimeOffset CreatedAt { get; init; }
        public int AttemptCount { get; init; }
    }
}
