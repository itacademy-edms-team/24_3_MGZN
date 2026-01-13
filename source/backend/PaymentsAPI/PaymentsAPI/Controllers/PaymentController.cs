using Microsoft.AspNetCore.Mvc;
using PaymentsAPI.Dtos;

namespace PaymentsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaymentController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
        {
            Console.WriteLine($"[PaymentsAPI] Initiating payment for OrderId: {request.OrderId}");
            Console.WriteLine($"[PaymentsAPI] Card Info: Number: {request.CardNumber}, Expiry: {request.ExpiryDate}, CVV: {request.Cvv}, Holder: {request.CardholderName}");

            await Task.Delay(10000);

            var status = "Payed";

            var webhookUrl = _configuration["WebhookUrl"] ?? "https://localhost:7275/api/webhooks/payment-confirmation";
            var webhookPayload = new PaymentConfirmationWebhookDto
            {
                OrderId = request.OrderId,
                Status = status
            };

            try
            {
                var webhookResponse = await _httpClient.PostAsJsonAsync(webhookUrl, webhookPayload);

                if (webhookResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[PaymentsAPI] Webhook sent successfully for OrderId: {request.OrderId}, Status: {status}");
                }
                else
                {
                    Console.WriteLine($"[PaymentsAPI] Webhook failed for OrderId: {request.OrderId}, Status: {status}, Code: {webhookResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentsAPI] Error sending webhook for OrderId {request.OrderId}: {ex.Message}");
            }

            return Ok(new { message = "Payment initiated, webhook will be sent." });
        }
    }
}
