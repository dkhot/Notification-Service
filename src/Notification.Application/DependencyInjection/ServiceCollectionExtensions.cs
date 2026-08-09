using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Callbacks;
using Notification.Application.Notifications;

namespace Notification.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
        {
            services.AddScoped<ICreateNotificationHandler, CreateNotificationHandler>();
            services.AddScoped<IProcessNotificationHandler, ProcessNotificationHandler>();
            services.AddScoped<IHandleNotificationCompletedHandler, HandleNotificationCompletedHandler>();
            services.AddScoped<IDispatchPendingCallbackHandler, DispatchPendingCallbackHandler>();

            return services;
        }
    }
}
