using Notification.Application.Abstractions;

namespace Notification.Tests.Notifications
{
    public sealed partial class CreateNotificationHandlerTests
    {
        private sealed class FakeMessageQueue : IMessageQueue
        {
            public List<object> EnqueuedMessages { get; } = new();

            public Task EnqueueAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
            {
                EnqueuedMessages.Add(message!);
                return Task.CompletedTask;
            }

            public Task<QueueMessage<T>?> DequeueAsync<T>(CancellationToken cancellationToken = default) where T : class =>
                Task.FromResult<QueueMessage<T>?>(null);

            public Task MarkProcessedAsync(Guid messageId, bool success, string? error = null, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }
}
