using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notification.Infrastructure.Persistence
{
    public sealed class SqlMessageQueue : IMessageQueue
    {
        private readonly IDbContextFactory<NotificationDbContext> _dbContextFactory;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public SqlMessageQueue(IDbContextFactory<NotificationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task EnqueueAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var payload = JsonSerializer.Serialize(message, _jsonOptions);
            var queueItem = new MessageQueueItem
            {
                Id = Guid.NewGuid(),
                MessageType = typeof(T).FullName ?? typeof(T).Name,
                Payload = payload,
                CreatedAt = DateTimeOffset.UtcNow,
                AttemptCount = 0
            };

            dbContext.MessageQueueItems.Add(queueItem);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<QueueMessage<T>?> DequeueAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;
            var messageType = typeof(T).FullName ?? typeof(T).Name;

            var queueItem = await dbContext.MessageQueueItems
                .Where(item => item.MessageType == messageType && item.ProcessedAt == null && (item.LockedUntil == null || item.LockedUntil < now))
                .OrderBy(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (queueItem == null)
            {
                return null;
            }

            queueItem.LockedUntil = now.AddSeconds(30);
            queueItem.AttemptCount++;
            await dbContext.SaveChangesAsync(cancellationToken);

            var payload = JsonSerializer.Deserialize<T>(queueItem.Payload, _jsonOptions);
            if (payload == null)
            {
                queueItem.ProcessedAt = DateTimeOffset.UtcNow;
                queueItem.Success = false;
                queueItem.Error = "Unable to deserialize queue payload.";
                await dbContext.SaveChangesAsync(cancellationToken);
                return null;
            }

            return new QueueMessage<T>
            {
                Id = queueItem.Id,
                Payload = payload,
                CreatedAt = queueItem.CreatedAt,
                AttemptCount = queueItem.AttemptCount
            };
        }

        public async Task MarkProcessedAsync(Guid messageId, bool success, string? error = null, CancellationToken cancellationToken = default)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var queueItem = await dbContext.MessageQueueItems.FindAsync(new object[] { messageId }, cancellationToken);
            if (queueItem == null)
            {
                return;
            }

            queueItem.ProcessedAt = DateTimeOffset.UtcNow;
            queueItem.Success = success;
            queueItem.Error = error;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
