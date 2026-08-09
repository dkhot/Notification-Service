using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using Notification.Domain.Repositories;

namespace Notification.Infrastructure.Persistence
{
    public sealed class SourceConfigurationRepository : ISourceConfigurationRepository
    {
        private readonly NotificationDbContext _db;

        public SourceConfigurationRepository(NotificationDbContext db)
        {
            _db = db;
        }

        public Task<SourceConfiguration?> GetBySourceIdAsync(string sourceId, CancellationToken cancellationToken = default) =>
            _db.SourceConfigurations.FirstOrDefaultAsync(s => s.SourceId == sourceId, cancellationToken);
    }
}
