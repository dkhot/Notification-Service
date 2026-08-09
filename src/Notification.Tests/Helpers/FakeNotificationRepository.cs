using Notification.Domain.Repositories;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Tests.Helpers
{

    internal sealed class FakeNotificationRepository : INotificationRepository
    {
        private readonly Dictionary<Guid, NotificationEntity> _byId = new();
        private readonly HashSet<string> _messageIds = new();

        public Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_messageIds.Contains(messageId));

        public Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(id, out var n) ? n : null);

        public Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
        {
            _byId[notification.Id] = notification;
            _messageIds.Add(notification.MessageId);
            return Task.CompletedTask;
        }
    }

}
