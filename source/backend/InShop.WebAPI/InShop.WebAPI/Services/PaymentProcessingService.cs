using Contracts.Dtos;

namespace InShop.WebAPI.Services
{
    public class PaymentProcessingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _paymentsApiBaseUrl;
        private readonly ILogger<PaymentProcessingService> _logger;

        public PaymentProcessingService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PaymentProcessingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _paymentsApiBaseUrl = configuration["PaymentsAPI:BaseUrl"] ?? "http://localhost:5001";
        }

        public async Task InitiatePaymentAsync(PaymentRequestDto paymentData)
        {
            var requestUri = $"{_paymentsApiBaseUrl}/api/Payment/initiate";

            var request = new
            {
                OrderId = paymentData.OrderId,
                CardNumber = paymentData.CardNumber,
                ExpiryDate = paymentData.ExpiryDate,
                Cvv = paymentData.Cvv,
                CardholderName = paymentData.CardholderName
            };

            var response = await _httpClient.PostAsJsonAsync(requestUri, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PaymentsAPI returned error on initiate for OrderId={OrderId}: {StatusCode}",
                    paymentData.OrderId,
                    response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            _logger.LogInformation("Payment initiation request sent for OrderId={OrderId}", paymentData.OrderId);
        }
    }
}
