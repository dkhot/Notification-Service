using SharedKernel;

namespace Notification.Application.Callbacks
{
    public interface IHandleNotificationCompletedHandler
    {
        Task HandleAsync(NotificationCompletedMessage message, CancellationToken cancellationToken = default);
    }
}
