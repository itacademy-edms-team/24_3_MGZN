using Contracts.Dtos;

namespace InShop.WebAPI.Services
{
    public class PaymentProcessingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _paymentsApiBaseUrl;

        public PaymentProcessingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
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

            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var response = await _httpClient.PostAsJsonAsync(requestUri, request);

                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[PaymentProcessingService] PaymentsAPI returned error on initiate: {response.StatusCode}");
                        }
                        else
                        {
                            Console.WriteLine($"[PaymentProcessingService] Initiation request sent for OrderId: {paymentData.OrderId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PaymentProcessingService] Error calling PaymentsAPI initiate: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentProcessingService] Error calling PaymentsAPI initiate: {ex.Message}");
            }
        }
    }
}
