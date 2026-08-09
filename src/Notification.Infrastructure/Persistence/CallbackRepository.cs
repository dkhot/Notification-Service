using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using Notification.Domain.Repositories;
using SharedKernel;

namespace Notification.Infrastructure.Persistence
{
    public sealed class CallbackRepository : ICallbackRepository
    {
        private readonly NotificationDbContext _db;

        public CallbackRepository(NotificationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Callback callback, CancellationToken cancellationToken = default)
        {
            await _db.Callbacks.AddAsync(callback, cancellationToken);
        }

        public Task<Callback?> GetNextDueAsync(CancellationToken cancellationToken = default) =>
            _db.Callbacks
                .Where(c => c.Status == CallbackStatus.Pending && c.NextAttemptAt <= DateTimeOffset.UtcNow)
                .OrderBy(c => c.NextAttemptAt)
                .FirstOrDefaultAsync(cancellationToken);
    }
}
