using Notification.Domain.Repositories;

namespace Notification.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly NotificationDbContext _db;

        public UnitOfWork(NotificationDbContext db)
        {
            _db = db;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);
    }
}
