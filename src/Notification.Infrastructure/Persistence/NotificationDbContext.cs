using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Persistence
{
    public sealed class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
        public DbSet<Callback> Callbacks => Set<Callback>();
        public DbSet<SourceConfiguration> SourceConfigurations => Set<SourceConfiguration>();
        public DbSet<MessageQueueItem> MessageQueueItems => Set<MessageQueueItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NotificationEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MessageId).IsUnique();
                entity.Property(e => e.MessageId).IsRequired();
                entity.Property(e => e.SourceId).IsRequired();
                entity.Property(e => e.EventType).IsRequired();
                entity.Property(e => e.Recipient).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
                entity.Property(e => e.Channel).HasConversion<string>().IsRequired();
            });

            modelBuilder.Entity<Callback>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SourceId).IsRequired();
                entity.Property(e => e.WebhookUrl).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            });

            modelBuilder.Entity<SourceConfiguration>(entity =>
            {
                entity.HasKey(e => e.SourceId);
                entity.Property(e => e.Application).IsRequired();
                entity.Property(e => e.WebhookUrl).IsRequired();
            });

            modelBuilder.Entity<MessageQueueItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MessageType).IsRequired();
                entity.Property(e => e.Payload).IsRequired();
            });
        }
    }
}
