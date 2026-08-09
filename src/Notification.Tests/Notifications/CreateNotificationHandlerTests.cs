using FluentAssertions;
using Notification.Application.Notifications;
using Notification.Domain.Exceptions;
using SharedKernel;
using NotificationEntity = Notification.Domain.Entities.Notification;
using SourceConfiguration = Notification.Domain.Entities.SourceConfiguration;
using Notification.Tests.Helpers;

namespace Notification.Tests.Notifications
{
    public sealed partial class CreateNotificationHandlerTests
    {
        [Fact]
        public async Task HandleAsync_CreatesNotification_WhenRequestIsValid()
        {
            var notifications = new FakeNotificationRepository();
            var sourceConfigurations = new FakeSourceConfigurationRepository();
            sourceConfigurations.Add(new SourceConfiguration
            {
                SourceId = "inventory",
                Application = "Inventory",
                WebhookUrl = "https://example.com/webhook/inventory",
                Enabled = true
            });
            var unitOfWork = new FakeUnitOfWork();
            var queue = new FakeMessageQueue();
            var handler = new CreateNotificationHandler(notifications, sourceConfigurations, unitOfWork, queue);

            var request = new CreateNotificationCommand
            {
                MessageId = "msg-1",
                SourceId = "inventory",
                EventType = "OrderCreated",
                Channel = NotificationChannel.Email,
                Recipient = "user@example.com",
                Subject = "Order created",
                Body = "Your order has been created.",
                Payload = "{\"orderId\":123}"
            };

            var notificationId = await handler.HandleAsync(request);

            notificationId.Should().NotBeEmpty();
            var stored = await notifications.GetByIdAsync(notificationId);
            stored.Should().NotBeNull();
            stored!.Status.Should().Be(NotificationStatus.Queued);
            queue.EnqueuedMessages.Should().ContainSingle();
        }

        [Fact]
        public async Task HandleAsync_Throws_WhenMessageIdAlreadyExists()
        {
            var notifications = new FakeNotificationRepository();
            var existing = NotificationEntity.Create("msg-1", "inventory", "ExistingEvent", NotificationChannel.Email, "user@example.com", null, null, null);
            await notifications.AddAsync(existing);

            var sourceConfigurations = new FakeSourceConfigurationRepository();
            sourceConfigurations.Add(new SourceConfiguration
            {
                SourceId = "inventory",
                Application = "Inventory",
                WebhookUrl = "https://example.com/webhook/inventory",
                Enabled = true
            });

            var handler = new CreateNotificationHandler(notifications, sourceConfigurations, new FakeUnitOfWork(), new FakeMessageQueue());
            var request = new CreateNotificationCommand
            {
                MessageId = "msg-1",
                SourceId = "inventory",
                EventType = "OrderCreated",
                Channel = NotificationChannel.Email,
                Recipient = "user@example.com"
            };

            await Assert.ThrowsAsync<DuplicateNotificationException>(() => handler.HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_Throws_WhenSourceIsNotRegistered()
        {
            var handler = new CreateNotificationHandler(
                new FakeNotificationRepository(),
                new FakeSourceConfigurationRepository(),
                new FakeUnitOfWork(),
                new FakeMessageQueue());

            var request = new CreateNotificationCommand
            {
                MessageId = "msg-1",
                SourceId = "unknown-source",
                EventType = "OrderCreated",
                Channel = NotificationChannel.Email,
                Recipient = "user@example.com"
            };

            await Assert.ThrowsAsync<SourceNotRegisteredException>(() => handler.HandleAsync(request));
        }
    }
}
