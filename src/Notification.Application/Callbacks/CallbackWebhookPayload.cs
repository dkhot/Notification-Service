using SharedKernel;

namespace Notification.Application.Callbacks
{
    /// <summary>The JSON payload posted to a consumer's registered webhook.</summary>
    public sealed class CallbackWebhookPayload
    {
        public Guid NotificationId { get; init; }
        public string SourceId { get; init; } = null!;
        public string WebhookUrl { get; init; } = null!;
        public int RetryCount { get; init; }
        public CallbackStatus Status { get; init; }
    }
}
