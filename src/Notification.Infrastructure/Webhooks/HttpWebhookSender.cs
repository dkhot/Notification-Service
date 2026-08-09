using Notification.Application.Abstractions;
using System.Net.Http.Json;

namespace Notification.Infrastructure.Webhooks
{
    public sealed class HttpWebhookSender : IWebhookSender
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpWebhookSender(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<WebhookSendResult> SendAsync(string webhookUrl, object payload, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(webhookUrl, payload, cancellationToken);

                return response.IsSuccessStatusCode
                    ? WebhookSendResult.Ok()
                    : WebhookSendResult.Failed(response.ReasonPhrase ?? "Webhook returned non-success status.");
            }
            catch (Exception exception)
            {
                return WebhookSendResult.Failed(exception.Message);
            }
        }
    }
}
