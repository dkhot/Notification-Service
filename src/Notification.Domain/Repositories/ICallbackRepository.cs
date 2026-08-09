using Notification.Domain.Entities;

namespace Notification.Domain.Repositories
{
    public interface ICallbackRepository
    {
        Task AddAsync(Callback callback, CancellationToken cancellationToken = default);
        Task<Callback?> GetNextDueAsync(CancellationToken cancellationToken = default);
    }
}
