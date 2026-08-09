using Notification.Domain.Entities;

namespace Notification.Infrastructure.Persistence
{
    public static class NotificationDbContextExtensions
    {
        public static void EnsureSeedData(this NotificationDbContext context)
        {
            if (context.SourceConfigurations.Any())
            {
                return;
            }

            context.SourceConfigurations.AddRange(
                new SourceConfiguration
                {
                    SourceId = "inventory",
                    Application = "Inventory",
                    WebhookUrl = "https://example.com/webhook/inventory",
                    Enabled = true
                },
                new SourceConfiguration
                {
                    SourceId = "payments",
                    Application = "Payments",
                    WebhookUrl = "https://example.com/webhook/payments",
                    Enabled = true
                }
            );

            context.SaveChanges();
        }
    }
}
