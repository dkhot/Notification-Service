using SharedKernel;

namespace Notification.Application.Abstractions
{
    public interface INotificationProvider
    {
        string Name { get; }
        Task SendAsync(NotificationRequestMessage request, CancellationToken cancellationToken = default);
    }
}
