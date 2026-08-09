using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure.HealthChecks
{
    public sealed class NotificationDbContextHealthCheck : IHealthCheck
    {
        private readonly IDbContextFactory<NotificationDbContext> _dbContextFactory;

        public NotificationDbContextHealthCheck(IDbContextFactory<NotificationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Unable to connect to the notification database.");
        }
    }
}
