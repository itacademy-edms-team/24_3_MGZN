using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InShop.WebAPI.Services.Payment.Clients
{
    /// <summary>
    /// HTTP-клиент к API ЮKassa v3.
    /// Аутентификация: Basic Auth (shopId:secretKey в Base64).
    /// </summary>
    public class YooKassaClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<YooKassaClient> _logger;

        // Имена полей заданы через [JsonPropertyName] (snake_case как в API ЮKassa).
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public YooKassaClient(HttpClient httpClient, IConfiguration configuration, ILogger<YooKassaClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var baseUrl = _configuration["Payment:YooKassa:BaseUrl"] ?? "https://api.yookassa.ru/v3";
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

            // Заголовок Authorization: Basic base64(shopId:secretKey)
            // ЮKassa принимает логин = ShopId, пароль = SecretKey (см. документацию по формату запросов).
            var shopId = _configuration["Payment:YooKassa:ShopId"] ?? string.Empty;
            var secretKey = _configuration["Payment:YooKassa:SecretKey"] ?? string.Empty;
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secretKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        /// <summary>
        /// Создаёт платёж в ЮKassa. Возвращает id платежа и confirmation_url для редиректа пользователя.
        /// </summary>
        public async Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            // Idempotence-Key обязателен для POST — защита от дублей при повторе запроса.
            var idempotenceKey = Guid.NewGuid().ToString();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "payments");
            httpRequest.Headers.Add("Idempotence-Key", idempotenceKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("ЮKassa: создание платежа, Idempotence-Key={Key}", idempotenceKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ЮKassa CreatePayment ошибка {Status}: {Body}", response.StatusCode, body);
                throw new HttpRequestException($"ЮKassa CreatePayment failed ({(int)response.StatusCode}): {body}");
            }

            // Парсим JSON: важны id (идентификатор платежа) и confirmation.confirmation_url (куда редиректить).
            var payment = JsonSerializer.Deserialize<PaymentResponse>(body, JsonOptions)
                ?? throw new HttpRequestException("ЮKassa: пустой ответ при создании платежа");

            return payment;
        }

        /// <summary>
        /// Запрашивает актуальный статус платежа (pending, waiting_for_capture, succeeded, canceled).
        /// </summary>
        public async Task<PaymentStatusResponse> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"payments/{paymentId}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ЮKassa GetPayment ошибка {Status}: {Body}", response.StatusCode, body);
                throw new HttpRequestException($"ЮKassa GetPayment failed ({(int)response.StatusCode}): {body}");
            }

            var payment = JsonSerializer.Deserialize<PaymentStatusResponse>(body, JsonOptions)
                ?? throw new HttpRequestException("ЮKassa: пустой ответ при запросе статуса");

            return payment;
        }
    }
}
