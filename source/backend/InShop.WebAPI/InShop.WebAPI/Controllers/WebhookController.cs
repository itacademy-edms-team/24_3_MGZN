using InShopBLLayer.Abstractions;
using InShop.WebAPI.Services.Payment;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhookController : ControllerBase
    {
        private readonly InShopDbModels.Abstractions.IOrderRepository _orderRepository;
        private readonly IInventoryReservationService _inventoryReservationService;
        private readonly IOrderStatusEmailNotifier _emailNotifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            InShopDbModels.Abstractions.IOrderRepository orderRepository,
            IInventoryReservationService inventoryReservationService,
            IOrderStatusEmailNotifier emailNotifier,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<WebhookController> logger)
        {
            _orderRepository = orderRepository;
            _inventoryReservationService = inventoryReservationService;
            _emailNotifier = emailNotifier;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _environment = environment;
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
            if (!ValidateWebhookSecret("Payment:YooKassa:WebhookSecret"))
            {
                _logger.LogWarning("ЮKassa webhook rejected: invalid secret");
                return Unauthorized();
            }

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
            if (!ValidateWebhookSecret("Payment:Mock:WebhookSecret"))
            {
                _logger.LogWarning("Мок webhook rejected: invalid secret");
                return Unauthorized();
            }

            var order = await _orderRepository.GetOrderById(dto.OrderId);

            if (order != null)
            {
                if (order.OrderStatus == "Unpayed")
                {
                    if (string.Equals(dto.Status, "Payed", StringComparison.OrdinalIgnoreCase))
                    {
                        var orderWithItems = await _orderRepository.GetOrderById(dto.OrderId) ?? order;
                        foreach (var item in orderWithItems.OrderItems ?? Enumerable.Empty<InShopDbModels.Models.OrderItem>())
                        {
                            await _inventoryReservationService.ReserveAsync(item.ProductId, item.QuantityItem);
                        }
                    }

                    order.OrderStatus = dto.Status;
                    // При успешной оплате обновляем и PayStatus (как в ЮKassa TryMarkOrderAsPaidAsync).
                    if (string.Equals(dto.Status, "Payed", StringComparison.OrdinalIgnoreCase))
                    {
                        order.PayStatus = "Payed";
                    }

                    await _orderRepository.UpdateOrder(order);
                    _logger.LogInformation("Мок webhook: заказ {OrderId} → OrderStatus={Status}, PayStatus={PayStatus}",
                        dto.OrderId, dto.Status, order.PayStatus);

                    if (string.Equals(dto.Status, "Payed", StringComparison.OrdinalIgnoreCase))
                    {
                        var orderForEmail = await _orderRepository.GetOrderById(dto.OrderId);
                        if (orderForEmail != null)
                        {
                            await _emailNotifier.SendStatusUpdateAsync(orderForEmail);
                        }
                    }
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

        private bool ValidateWebhookSecret(string configurationKey)
        {
            var expectedSecret = _configuration[configurationKey];
            if (string.IsNullOrWhiteSpace(expectedSecret))
            {
                return _environment.IsDevelopment();
            }

            var actualSecret = Request.Headers["X-Webhook-Secret"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(actualSecret))
            {
                return false;
            }

            var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
            var actualBytes = Encoding.UTF8.GetBytes(actualSecret);
            return expectedBytes.Length == actualBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
    }
}
