using Microsoft.EntityFrameworkCore;
using Notification.Domain.Repositories;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Persistence
{
    public sealed class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _db;

        public NotificationRepository(NotificationDbContext db)
        {
            _db = db;
        }

        public Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default) =>
            _db.Notifications.AnyAsync(n => n.MessageId == messageId, cancellationToken);

        public Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        public async Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
        {
            await _db.Notifications.AddAsync(notification, cancellationToken);
        }
    }
}
