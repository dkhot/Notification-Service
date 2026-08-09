using SharedKernel;

namespace Notification.Application.Abstractions
{
    public interface INotificationProviderResolver
    {
        INotificationProvider Resolve(NotificationChannel channel);
    }
}
