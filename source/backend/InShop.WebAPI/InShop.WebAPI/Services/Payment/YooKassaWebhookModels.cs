using System.Text.Json.Serialization;

namespace InShop.WebAPI.Services.Payment
{
    /// <summary>
    /// Тело HTTP-уведомления от ЮKassa (webhook).
    /// Пример: event = "payment.succeeded", object — платёж с metadata.order_id.
    /// </summary>
    public class WebhookPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public WebhookPaymentObject? Object { get; set; }
    }

    public class WebhookPaymentObject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
