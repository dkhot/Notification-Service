using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure.Extensions
{
    public static class HostExtensions
    {
        public static void EnsureNotificationDatabase(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            db.Database.EnsureCreated();
            db.EnsureSeedData();
        }
    }
}
