namespace Notification.Application.Abstractions
{
    public interface IWebhookSender
    {
        Task<WebhookSendResult> SendAsync(string webhookUrl, object payload, CancellationToken cancellationToken = default);
    }

    public sealed class WebhookSendResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }

        public static WebhookSendResult Ok() => new() { Success = true };
        public static WebhookSendResult Failed(string error) => new() { Success = false, Error = error };
    }
}
