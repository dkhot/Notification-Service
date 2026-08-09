using Notification.Application.Abstractions;
using SharedKernel;
using System.Text;

namespace Notification.Infrastructure.Providers
{
    public sealed class FileSmsProvider : INotificationProvider
    {
        public string Name => "FileSms";

        public async Task SendAsync(NotificationRequestMessage request, CancellationToken cancellationToken = default)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "storage", "sms");
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, $"{request.NotificationId}.txt");
            var builder = new StringBuilder();
            builder.AppendLine("SMS Notification");
            builder.AppendLine($"MessageId: {request.MessageId}");
            builder.AppendLine($"SourceId: {request.SourceId}");
            builder.AppendLine($"Recipient: {request.Recipient}");
            builder.AppendLine($"Body: {request.Body}");
            builder.AppendLine($"Payload: {request.Payload}");
            await File.WriteAllTextAsync(filePath, builder.ToString(), cancellationToken);
        }
    }
}
