namespace Notification.Domain.Entities
{
    public sealed class SourceConfiguration
    {
        public string SourceId { get; set; } = null!;
        public string Application { get; set; } = null!;
        public string WebhookUrl { get; set; } = null!;
        public bool Enabled { get; set; }
    }
}
