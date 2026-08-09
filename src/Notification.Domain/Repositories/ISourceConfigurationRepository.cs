using Notification.Domain.Entities;

namespace Notification.Domain.Repositories
{
    public interface ISourceConfigurationRepository
    {
        Task<SourceConfiguration?> GetBySourceIdAsync(string sourceId, CancellationToken cancellationToken = default);
    }
}
