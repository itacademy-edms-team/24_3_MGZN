using System.Text.Json.Serialization;

namespace InShop.WebAPI.Services.Payment.Clients
{
    /// <summary>
    /// Тело запроса POST /v3/payments в API ЮKassa.
    /// Документация: https://yookassa.ru/developers/api#create_payment
    /// </summary>
    public class CreatePaymentRequest
    {
        [JsonPropertyName("amount")]
        public YooKassaAmount Amount { get; set; } = new();

        /// <summary>true — списать деньги сразу после успешной оплаты.</summary>
        [JsonPropertyName("capture")]
        public bool Capture { get; set; } = true;

        [JsonPropertyName("confirmation")]
        public YooKassaConfirmation Confirmation { get; set; } = new();

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Произвольные данные (все значения — строки).
        /// order_id нужен, чтобы в вебхуке понять, какой заказ оплачен.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class YooKassaAmount
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = "0.00";

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "RUB";
    }

    public class YooKassaConfirmation
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "redirect";

        [JsonPropertyName("return_url")]
        public string ReturnUrl { get; set; } = string.Empty;
    }

    /// <summary>Ответ ЮKassa при создании платежа — нас интересуют id и confirmation.confirmation_url.</summary>
    public class PaymentResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("confirmation")]
        public YooKassaConfirmationResponse? Confirmation { get; set; }
    }

    public class YooKassaConfirmationResponse
    {
        [JsonPropertyName("confirmation_url")]
        public string? ConfirmationUrl { get; set; }
    }

    /// <summary>Упрощённый ответ GET /v3/payments/{id} — только статус.</summary>
    public class PaymentStatusResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
