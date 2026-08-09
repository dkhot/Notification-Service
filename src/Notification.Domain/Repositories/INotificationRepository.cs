using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Domain.Repositories
{
    public interface INotificationRepository
    {
        Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
        Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
    }
}
