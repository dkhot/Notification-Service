using Notification.Application.Notifications;
using Notification.Domain.Exceptions;

namespace Notification.Api.Endpoints
{
    public static class NotificationEndpoints
    {
        public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/notifications", async (
                CreateNotificationCommand request,
                ICreateNotificationHandler handler,
                ILogger<Program> logger) =>
            {
                try
                {
                    var id = await handler.HandleAsync(request);
                    return Results.Accepted($"/notifications/{id}", new { id });
                }
                catch (ArgumentException exception)
                {
                    logger.LogWarning(exception, "Invalid request.");
                    return Results.BadRequest(new { error = exception.Message });
                }
                catch (SourceNotRegisteredException exception)
                {
                    logger.LogWarning(exception, "Request validation failed.");
                    return Results.BadRequest(new { error = exception.Message });
                }
                catch (DuplicateNotificationException exception)
                {
                    logger.LogWarning(exception, "Duplicate notification request.");
                    return Results.Conflict(new { error = exception.Message });
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Unable to create notification.");
                    return Results.Problem("Unable to create notification.");
                }
            });

            return app;
        }
    }
}
