using SharedKernel;

namespace Notification.Application.Notifications
{
    public sealed class CreateNotificationCommand
    {
        public string MessageId { get; set; } = null!;
        public string SourceId { get; set; } = null!;
        public string EventType { get; set; } = null!;
        public NotificationChannel Channel { get; set; }
        public string Recipient { get; set; } = null!;
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? Payload { get; set; }
    }
}
