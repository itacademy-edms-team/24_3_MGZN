using Contracts.Dtos;
using InShop.WebAPI.Services;

namespace InShop.WebAPI.Services.Payment
{
    /// <summary>
    /// Мок-оплата через отдельный сервис PaymentsAPI (задержка + вебхук payment-confirmation).
    /// Используется, когда Payment:Provider != "YooKassa".
    /// </summary>
    public class MockPaymentService
    {
        private readonly PaymentProcessingService _paymentProcessingService;
        private readonly ILogger<MockPaymentService> _logger;

        public MockPaymentService(PaymentProcessingService paymentProcessingService, ILogger<MockPaymentService> logger)
        {
            _paymentProcessingService = paymentProcessingService;
            _logger = logger;
        }

        /// <summary>
        /// Отправляет данные карты в PaymentsAPI; результат придёт асинхронно через вебхук.
        /// </summary>
        public async Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentRequestDto request)
        {
            _logger.LogInformation("Мок-оплата: инициация для OrderId={OrderId}", request.OrderId);

            await _paymentProcessingService.InitiatePaymentAsync(request);

            return new PaymentInitiationResult
            {
                Message = "Payment process initiated successfully. Wait for webhook confirmation."
            };
        }
    }
}
