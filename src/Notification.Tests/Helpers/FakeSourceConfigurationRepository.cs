using Notification.Domain.Repositories;
using SourceConfiguration = Notification.Domain.Entities.SourceConfiguration;

namespace Notification.Tests.Helpers
{
    internal sealed class FakeSourceConfigurationRepository : ISourceConfigurationRepository
    {
        private readonly Dictionary<string, SourceConfiguration> _bySourceId = new();

        public void Add(SourceConfiguration config) => _bySourceId[config.SourceId] = config;

        public Task<SourceConfiguration?> GetBySourceIdAsync(string sourceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_bySourceId.TryGetValue(sourceId, out var c) ? c : null);
    }
}

