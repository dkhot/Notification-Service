using Notification.Domain.Repositories;

namespace Notification.Tests.Helpers
{

    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

