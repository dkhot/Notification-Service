using SharedKernel;

namespace Notification.Application.Notifications
{
    public interface IProcessNotificationHandler
    {
        Task HandleAsync(NotificationRequestMessage request, CancellationToken cancellationToken = default);
    }
}
