namespace Notification.Application.Notifications
{
    public interface ICreateNotificationHandler
    {
        Task<Guid> HandleAsync(CreateNotificationCommand request, CancellationToken cancellationToken = default);
    }
}
