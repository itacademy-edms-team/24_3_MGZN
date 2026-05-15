using InShop.WebAPI.Services.Payment;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhookController : ControllerBase
    {
        private readonly InShopDbModels.Abstractions.IOrderRepository _orderRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            InShopDbModels.Abstractions.IOrderRepository orderRepository,
            IServiceProvider serviceProvider,
            ILogger<WebhookController> logger)
        {
            _orderRepository = orderRepository;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public class PaymentConfirmationWebhookDto
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        /// <summary>
        /// Webhook от ЮKassa (настраивается в личном кабинете / через NotificationUrl).
        /// Маршрут: POST /api/webhook/yookassa (отдельный префикс, как в документации задачи).
        /// Важно вернуть 200 OK — иначе ЮKassa будет повторять доставку уведомления.
        /// При 4xx/5xx возможны многократные дубли и задержки обновления статуса заказа.
        /// </summary>
        [HttpPost("/api/webhook/yookassa")]
        public async Task<IActionResult> HandleYooKassaWebhook([FromBody] JsonElement payload)
        {
            _logger.LogInformation("ЮKassa webhook received");

            var yooKassaService = _serviceProvider.GetService<YooKassaPaymentService>();
            if (yooKassaService == null)
            {
                _logger.LogWarning("ЮKassa webhook: YooKassaPaymentService не зарегистрирован (Payment:Provider != YooKassa?)");
                return Ok();
            }

            WebhookPayload? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<WebhookPayload>(
                    payload.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ЮKassa webhook: не удалось разобрать JSON");
                return Ok();
            }

            if (webhook != null)
            {
                await yooKassaService.ProcessWebhookAsync(webhook);
            }

            return Ok();
        }

        [HttpPost("payment-confirmation")]
        public async Task<IActionResult> HandlePaymentConfirmation([FromBody] PaymentConfirmationWebhookDto dto)
        {
            _logger.LogInformation("Мок webhook: OrderId={OrderId}, Status={Status}", dto.OrderId, dto.Status);

            var order = await _orderRepository.GetOrderById(dto.OrderId);

            if (order != null)
            {
                if (order.OrderStatus == "Unpayed")
                {
                    order.OrderStatus = dto.Status;
                    // При успешной оплате обновляем и PayStatus (как в ЮKassa TryMarkOrderAsPaidAsync).
                    if (string.Equals(dto.Status, "Payed", StringComparison.OrdinalIgnoreCase))
                    {
                        order.PayStatus = "Payed";
                    }

                    await _orderRepository.UpdateOrder(order);
                    _logger.LogInformation("Мок webhook: заказ {OrderId} → OrderStatus={Status}, PayStatus={PayStatus}",
                        dto.OrderId, dto.Status, order.PayStatus);
                }
                else
                {
                    _logger.LogInformation("Мок webhook: заказ {OrderId} не Unpayed ({Status}), пропуск",
                        dto.OrderId, order.OrderStatus);
                }
            }
            else
            {
                _logger.LogWarning("Мок webhook: заказ {OrderId} не найден", dto.OrderId);
            }

            return Ok();
        }
    }
}
