using Notification.Application.Abstractions;
using SharedKernel;

namespace Notification.Infrastructure.Providers
{
    public sealed class NotificationProviderResolver(IEnumerable<INotificationProvider> providers) : INotificationProviderResolver
    {
        private readonly IEnumerable<INotificationProvider> _providers = providers;

        public INotificationProvider Resolve(NotificationChannel channel)
        {
            return channel switch
            {
                NotificationChannel.Email => _providers.FirstOrDefault(p => p.Name == "FileEmail") ?? throw new InvalidOperationException("No email provider is configured."),
                NotificationChannel.Sms => _providers.FirstOrDefault(p => p.Name == "FileSms") ?? throw new InvalidOperationException("No SMS provider is configured."),
                _ => throw new InvalidOperationException($"Unsupported channel {channel}.")
            };
        }
    }
}
