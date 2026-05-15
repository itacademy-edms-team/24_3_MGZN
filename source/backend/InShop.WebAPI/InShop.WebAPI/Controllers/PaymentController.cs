using Contracts.Dtos;
using InShop.WebAPI.Services.Payment;
using InShopDbModels.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly MockPaymentService _mockPaymentService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IConfiguration configuration,
            MockPaymentService mockPaymentService,
            IServiceProvider serviceProvider,
            ILogger<PaymentController> logger)
        {
            _configuration = configuration;
            _mockPaymentService = mockPaymentService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Текущий провайдер оплаты — фронт решает: редирект на ЮKassa или форма карты (мок).
        /// </summary>
        [HttpGet("provider")]
        public IActionResult GetProvider()
        {
            var provider = _configuration["Payment:Provider"] ?? "Mock";
            return Ok(new { provider });
        }

        /// <summary>
        /// Инициация оплаты ЮKassa: только orderId, без карты. Ответ — redirectUrl.
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateYooKassaPayment([FromBody] InitiatePaymentRequestDto request)
        {
            if (!IsYooKassaProvider())
            {
                return BadRequest(new { message = "Payment provider is not YooKassa. Use /api/Payment/process for mock." });
            }

            if (request.OrderId <= 0)
            {
                return BadRequest(new { message = "Invalid OrderId." });
            }

            var sessionId = GetSessionIdFromContext();
            if (!sessionId.HasValue)
            {
                return Unauthorized(new { message = "Session required." });
            }

            using var scope = _serviceProvider.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = await orderRepository.GetOrderById(request.OrderId);

            if (order == null)
            {
                return BadRequest(new { message = "Order not found." });
            }

            if (order.SessionId != sessionId.Value)
            {
                return Forbid();
            }

            if (order.OrderStatus != "Unpayed")
            {
                return BadRequest(new { message = "Order is not in 'Unpayed' status." });
            }

            var yooKassaService = scope.ServiceProvider.GetRequiredService<YooKassaPaymentService>();
            var result = await yooKassaService.InitiatePaymentAsync(order);

            return Ok(new
            {
                message = result.Message,
                redirectUrl = result.RedirectUrl,
                paymentId = result.PaymentId
            });
        }

        /// <summary>
        /// Синхронизация после return_url: paymentId читается из БД, статус запрашивается у ЮKassa.
        /// </summary>
        [HttpPost("confirm-yookassa")]
        public async Task<IActionResult> ConfirmYooKassaPayment([FromBody] ConfirmYooKassaPaymentRequestDto request)
        {
            if (!IsYooKassaProvider())
            {
                return BadRequest(new { message = "YooKassa is not the active payment provider." });
            }

            if (request.OrderId <= 0)
            {
                return BadRequest(new { message = "Invalid OrderId." });
            }

            var sessionId = GetSessionIdFromContext();
            if (!sessionId.HasValue)
            {
                return Unauthorized(new { message = "Session required." });
            }

            using var scope = _serviceProvider.CreateScope();
            var yooKassaService = scope.ServiceProvider.GetRequiredService<YooKassaPaymentService>();
            var result = await yooKassaService.ConfirmPaymentAfterReturnAsync(request.OrderId, sessionId.Value);

            var body = new
            {
                message = result.Message,
                orderStatus = result.OrderStatus,
                paymentStatus = result.PaymentStatus,
                isPaid = result.IsSuccess && !result.IsPending
            };

            return StatusCode(result.HttpStatus, body);
        }

        /// <summary>
        /// Мок-оплата: нужны данные карты. Для ЮKassa используйте POST /api/Payment/initiate.
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestDto request)
        {
            if (request.OrderId <= 0)
            {
                return BadRequest(new { message = "Invalid OrderId." });
            }

            if (IsYooKassaProvider())
            {
                return BadRequest(new
                {
                    message = "Use POST /api/Payment/initiate for YooKassa (card data is entered on YooKassa page)."
                });
            }

            if (string.IsNullOrWhiteSpace(request.CardNumber) ||
                string.IsNullOrWhiteSpace(request.ExpiryDate) ||
                string.IsNullOrWhiteSpace(request.Cvv) ||
                string.IsNullOrWhiteSpace(request.CardholderName))
            {
                return BadRequest(new { message = "Card fields are required for mock payment." });
            }

            using var scope = _serviceProvider.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = await orderRepository.GetOrderById(request.OrderId);
            if (order == null)
            {
                return BadRequest(new { message = "Order not found." });
            }

            if (order.OrderStatus != "Unpayed")
            {
                return BadRequest(new { message = "Order is not in 'Unpayed' status." });
            }

            var mockResult = await _mockPaymentService.InitiatePaymentAsync(request);
            return Ok(new { message = mockResult.Message });
        }

        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetPaymentStatus(int orderId)
        {
            using var scope = _serviceProvider.CreateScope();
            var paymentStatusService = scope.ServiceProvider.GetRequiredService<InShopBLLayer.Abstractions.IPaymentStatusService>();

            var status = await paymentStatusService.GetOrderSatusAsync(orderId);

            if (status == "NotFound")
            {
                return NotFound(new { error = "Order not found" });
            }

            return Ok(new { Status = status });
        }

        private bool IsYooKassaProvider()
        {
            return string.Equals(_configuration["Payment:Provider"], "YooKassa", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>SessionId из SessionMiddleware (cookie SessionToken).</summary>
        private int? GetSessionIdFromContext()
        {
            if (HttpContext.Items["SessionId"] is int sessionId)
            {
                return sessionId;
            }

            return null;
        }
    }
}
