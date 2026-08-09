using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Abstractions;
using Notification.Domain.Repositories;
using Notification.Infrastructure.HealthChecks;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Providers;
using Notification.Infrastructure.Webhooks;

namespace Notification.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, string connectionString)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

            services.AddDbContext<NotificationDbContext>(options => options.UseSqlServer(connectionString));
            services.AddDbContextFactory<NotificationDbContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<ICallbackRepository, CallbackRepository>();
            services.AddScoped<ISourceConfigurationRepository, SourceConfigurationRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IMessageQueue, SqlMessageQueue>();

            services.AddScoped<INotificationProvider, FileEmailProvider>();
            services.AddScoped<INotificationProvider, FileSmsProvider>();
            services.AddScoped<INotificationProviderResolver, NotificationProviderResolver>();

            services.AddScoped<IWebhookSender, HttpWebhookSender>();

            // Health check for infra
            services.AddHealthChecks().AddCheck<NotificationDbContextHealthCheck>("notification_db");

            // Use IHttpClientFactory for resiliency and testability
            services.AddHttpClient();

            return services;
        }
    }
}
