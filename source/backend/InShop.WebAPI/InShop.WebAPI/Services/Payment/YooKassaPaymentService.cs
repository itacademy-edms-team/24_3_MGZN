using InShop.WebAPI.Services.Payment.Clients;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;

namespace InShop.WebAPI.Services.Payment
{
    /// <summary>
    /// Бизнес-логика оплаты через ЮKassa: создание платежа, webhook и синхронизация после return_url.
    /// </summary>
    public class YooKassaPaymentService
    {
        private readonly YooKassaClient _client;
        private readonly IOrderRepository _orderRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<YooKassaPaymentService> _logger;

        public YooKassaPaymentService(
            YooKassaClient client,
            IOrderRepository orderRepository,
            IConfiguration configuration,
            ILogger<YooKassaPaymentService> logger)
        {
            _client = client;
            _orderRepository = orderRepository;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Создаёт платёж в ЮKassa, сохраняет YooKassaPaymentId в заказе, возвращает URL для редиректа.
        /// </summary>
        public async Task<PaymentInitiationResult> InitiatePaymentAsync(Order order)
        {
            var returnUrlBase = _configuration["Payment:YooKassa:ReturnUrl"]
                ?? _configuration["Frontend:BaseUrl"]
                ?? "http://localhost:3000/payment-confirmation";

            if (returnUrlBase.Contains("https://localhost:3000", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "ReturnUrl использует https://localhost:3000 — для npm start нужен http. Исправьте Payment:YooKassa:ReturnUrl.");
                returnUrlBase = returnUrlBase.Replace("https://localhost:3000", "http://localhost:3000", StringComparison.OrdinalIgnoreCase);
            }

            var returnUrl = $"{returnUrlBase.TrimEnd('/')}?orderId={order.OrderId}";

            var createRequest = new CreatePaymentRequest
            {
                Amount = new YooKassaAmount
                {
                    Value = order.OrderTotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    Currency = "RUB"
                },
                Capture = true,
                Confirmation = new YooKassaConfirmation
                {
                    Type = "redirect",
                    ReturnUrl = returnUrl
                },
                Description = $"Оплата заказа #{order.OrderId}",
                Metadata = new Dictionary<string, string>
                {
                    ["order_id"] = order.OrderId.ToString()
                }
            };

            _logger.LogInformation("ЮKassa: создание платежа OrderId={OrderId}, сумма={Amount}",
                order.OrderId, createRequest.Amount.Value);

            var payment = await _client.CreatePaymentAsync(createRequest);

            var confirmationUrl = payment.Confirmation?.ConfirmationUrl;
            if (string.IsNullOrEmpty(confirmationUrl))
            {
                throw new InvalidOperationException("ЮKassa не вернула confirmation_url.");
            }

            // paymentId храним в БД — при возврате с return_url фронт передаёт только orderId.
            order.YooKassaPaymentId = payment.Id;
            await _orderRepository.UpdateOrder(order);

            return new PaymentInitiationResult
            {
                RedirectUrl = confirmationUrl,
                PaymentId = payment.Id,
                Message = "Redirect to YooKassa payment page."
            };
        }

        /// <summary>
        /// Запасной путь после return_url: запрашиваем статус платежа в ЮKassa и при succeeded ставим Payed.
        /// Вызывается с фронта, когда webhook ещё не успел или туннель не настроен.
        /// </summary>
        public async Task<ConfirmPaymentResult> ConfirmPaymentAfterReturnAsync(int orderId, int sessionId)
        {
            var order = await _orderRepository.GetOrderById(orderId);
            if (order == null)
            {
                return ConfirmPaymentResult.NotFound("Заказ не найден.");
            }

            if (order.SessionId != sessionId)
            {
                _logger.LogWarning("Confirm: заказ {OrderId} не принадлежит сессии {SessionId}", orderId, sessionId);
                return ConfirmPaymentResult.Forbidden("Заказ не принадлежит текущей сессии.");
            }

            if (order.OrderStatus == "Payed")
            {
                return ConfirmPaymentResult.AlreadyPaid();
            }

            if (string.IsNullOrEmpty(order.YooKassaPaymentId))
            {
                return ConfirmPaymentResult.BadRequest("Для заказа не сохранён id платежа ЮKassa. Повторите оплату с экрана заказа.");
            }

            var paymentStatus = await _client.GetPaymentStatusAsync(order.YooKassaPaymentId);

            if (!string.Equals(paymentStatus.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Confirm: платёж {PaymentId} ещё не succeeded (статус {Status})",
                    order.YooKassaPaymentId, paymentStatus.Status);
                return ConfirmPaymentResult.Pending(paymentStatus.Status);
            }

            var marked = await TryMarkOrderAsPaidAsync(order, order.YooKassaPaymentId);
            return marked
                ? ConfirmPaymentResult.Success()
                : ConfirmPaymentResult.Pending(paymentStatus.Status);
        }

        /// <summary>
        /// Webhook payment.succeeded — основной способ обновить заказ.
        /// </summary>
        public async Task ProcessWebhookAsync(WebhookPayload payload)
        {
            // TODO: добавить проверку подписи вебхука в продакшене (WebhookSecret из конфига).

            if (!string.Equals(payload.Event, "payment.succeeded", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("ЮKassa webhook: событие {Event} пропущено", payload.Event);
                return;
            }

            var paymentObject = payload.Object;
            if (paymentObject?.Metadata == null ||
                !paymentObject.Metadata.TryGetValue("order_id", out var orderIdRaw) ||
                !int.TryParse(orderIdRaw, out var orderId))
            {
                _logger.LogWarning("ЮKassa webhook: не найден order_id в metadata");
                return;
            }

            var order = await _orderRepository.GetOrderById(orderId);
            if (order == null)
            {
                _logger.LogWarning("ЮKassa webhook: заказ {OrderId} не найден", orderId);
                return;
            }

            await TryMarkOrderAsPaidAsync(order, paymentObject.Id);
        }

        /// <summary>
        /// Общая логика: при успешной оплате обновляем OrderStatus и PayStatus.
        /// OrderStatus / PayStatus: Unpayed → Payed (как в мок-вебхуке и checkout).
        /// </summary>
        private async Task<bool> TryMarkOrderAsPaidAsync(Order order, string yooKassaPaymentId)
        {
            if (order.OrderStatus != "Unpayed")
            {
                // Примечание: заказ уже мог быть помечен webhook'ом — синхронизируем PayStatus на всякий случай.
                if (order.OrderStatus == "Payed" && order.PayStatus != PaidPayStatusValue)
                {
                    order.PayStatus = PaidPayStatusValue;
                    await _orderRepository.UpdateOrder(order);
                }

                _logger.LogInformation("Заказ {OrderId} уже {Status}, пропуск Payed", order.OrderId, order.OrderStatus);
                return order.OrderStatus == "Payed";
            }

            if (!string.IsNullOrEmpty(order.YooKassaPaymentId) &&
                !string.Equals(order.YooKassaPaymentId, yooKassaPaymentId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Заказ {OrderId}: ожидался платёж {Expected}, пришёл {Actual}",
                    order.OrderId, order.YooKassaPaymentId, yooKassaPaymentId);
                return false;
            }

            order.OrderStatus = "Payed";
            order.PayStatus = PaidPayStatusValue;
            if (string.IsNullOrEmpty(order.YooKassaPaymentId))
            {
                order.YooKassaPaymentId = yooKassaPaymentId;
            }

            await _orderRepository.UpdateOrder(order);
            _logger.LogInformation(
                "Заказ {OrderId}: OrderStatus=Payed, PayStatus={PayStatus} (платёж {PaymentId})",
                order.OrderId, order.PayStatus, yooKassaPaymentId);
            return true;
        }

        /// <summary>Согласовано с OrderStatus и мок-оплатой (не путать с дефолтом БД «Не оплачен» для новых строк без явной установки).</summary>
        private const string PaidPayStatusValue = "Payed";
    }

    /// <summary>Результат confirm-yookassa для контроллера.</summary>
    public class ConfirmPaymentResult
    {
        public bool IsSuccess { get; init; }
        public bool IsPending { get; init; }
        public int HttpStatus { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? OrderStatus { get; init; }
        public string? PaymentStatus { get; init; }

        public static ConfirmPaymentResult Success() => new()
        {
            IsSuccess = true,
            HttpStatus = 200,
            Message = "Order marked as paid.",
            OrderStatus = "Payed"
        };

        public static ConfirmPaymentResult AlreadyPaid() => new()
        {
            IsSuccess = true,
            HttpStatus = 200,
            Message = "Order already paid.",
            OrderStatus = "Payed"
        };

        public static ConfirmPaymentResult Pending(string paymentStatus) => new()
        {
            IsPending = true,
            HttpStatus = 200,
            Message = "Payment not completed yet.",
            OrderStatus = "Unpayed",
            PaymentStatus = paymentStatus
        };

        public static ConfirmPaymentResult NotFound(string message) => new()
        {
            HttpStatus = 404,
            Message = message
        };

        public static ConfirmPaymentResult Forbidden(string message) => new()
        {
            HttpStatus = 403,
            Message = message
        };

        public static ConfirmPaymentResult BadRequest(string message) => new()
        {
            HttpStatus = 400,
            Message = message
        };
    }
}
