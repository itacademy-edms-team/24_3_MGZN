using InShopBLLayer.Abstractions;
using InShopBLLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Contracts.Dtos;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IUserSessionService _userSessionService;
        private readonly OrderTrackingTokenService _trackingTokenService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            IUserSessionService userSessionService,
            OrderTrackingTokenService trackingTokenService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _userSessionService = userSessionService;
            _trackingTokenService = trackingTokenService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════
        // ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Получение валидированного SessionId
        // ═══════════════════════════════════════════════════════

        private async Task<(bool Success, int? SessionId, string? Error)> GetValidatedSessionIdAsync()
        {
            // Вариант 1: Если используешь middleware, SessionId уже в HttpContext.Items
            if (HttpContext.Items["SessionId"] is int sessionIdFromItems)
            {
                return (true, sessionIdFromItems, null);
            }

            // Вариант 2: Валидируем токен вручную
            var sessionToken = Request.Cookies["SessionToken"];

            if (string.IsNullOrEmpty(sessionToken) || !Guid.TryParse(sessionToken, out var tokenGuid))
            {
                return (false, null, "Session token missing or invalid");
            }

            var session = await _userSessionService.ValidateSessionAsync(tokenGuid);

            if (session == null)
            {
                return (false, null, "Session expired or invalid");
            }

            return (true, session.SessionId, null);
        }

        // ═══════════════════════════════════════════════════════
        // ДОБАВИТЬ ТОВАР В КОРЗИНУ
        // ═══════════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var (success, sessionId, error) = await GetValidatedSessionIdAsync();

            if (!success || !sessionId.HasValue)
            {
                _logger.LogWarning("AddToCart: {Error}", error);
                return Unauthorized(new { error = error });
            }

            try
            {
                var (orderId, orderItemId) = await _orderService.AddProductToCart(dto.ProductId, sessionId.Value);

                _logger.LogInformation(
                    "Product {ProductId} added to cart. SessionId={SessionId}, OrderId={OrderId}, OrderItemId={OrderItemId}",
                    dto.ProductId, sessionId, orderId, orderItemId);

                return Ok(new { orderId, orderItemId, message = "Товар успешно добавлен в корзину." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AddToCart: Business logic error");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddToCart: Unexpected error");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // УДАЛИТЬ ТОВАР ИЗ КОРЗИНЫ
        // ═══════════════════════════════════════════════════════

        [HttpDelete("{orderItemId}")]
        public async Task<IActionResult> RemoveFromCart(int orderItemId)
        {
            var (success, sessionId, error) = await GetValidatedSessionIdAsync();

            if (!success || !sessionId.HasValue)
            {
                return Unauthorized(new { error = error });
            }

            try
            {
                // Проверяем, что товар принадлежит сессии пользователя (безопасность)
                var belongsToSession = await _orderService.OrderItemBelongsToSessionAsync(orderItemId, sessionId.Value);

                if (!belongsToSession)
                {
                    _logger.LogWarning("RemoveFromCart: OrderItemId {OrderItemId} does not belong to SessionId {SessionId}",
                        orderItemId, sessionId);
                    return Forbid("Access denied");
                }

                await _orderService.RemoveProductFromCart(orderItemId);

                _logger.LogInformation("OrderItemId {OrderItemId} removed from cart. SessionId={SessionId}",
                    orderItemId, sessionId);

                return Ok(new { message = "Товар успешно удалён из корзины." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveFromCart: Unexpected error");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ОБНОВИТЬ КОЛИЧЕСТВО ТОВАРА
        // ═══════════════════════════════════════════════════════

        [HttpPut("updateQuantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityDto dto)
        {
            var (success, sessionId, error) = await GetValidatedSessionIdAsync();

            if (!success || !sessionId.HasValue)
            {
                return Unauthorized(new { error = error });
            }

            try
            {
                // Проверяем принадлежность товара сессии
                var belongsToSession = await _orderService.OrderItemBelongsToSessionAsync(dto.OrderItemId, sessionId.Value);

                if (!belongsToSession)
                {
                    return Forbid(error = "Access denied");
                }

                await _orderService.UpdateOrderItemQuantity(dto.OrderItemId, dto.Quantity);

                _logger.LogInformation("OrderItemId {OrderItemId} quantity updated to {Quantity}. SessionId={SessionId}",
                    dto.OrderItemId, dto.Quantity, sessionId);

                return Ok(new { message = "Количество успешно обновлено." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateQuantity: Unexpected error");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ОЧИСТИТЬ КОРЗИНУ
        // ═══════════════════════════════════════════════════════

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var (success, sessionId, error) = await GetValidatedSessionIdAsync();

            if (!success || !sessionId.HasValue)
            {
                return Unauthorized(new { error = error });
            }

            try
            {
                await _orderService.ClearCart(sessionId.Value);

                _logger.LogInformation("Cart cleared for SessionId={SessionId}", sessionId);

                return Ok(new { message = "Корзина очищена." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCart: Unexpected error");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ПОЛУЧИТЬ КОРЗИНУ
        // ═══════════════════════════════════════════════════════

        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            var (success, sessionId, error) = await GetValidatedSessionIdAsync();

            if (!success || !sessionId.HasValue)
            {
                // Возвращаем пустую корзину, если сессия не валидна (не 401, чтобы не ломать UI)
                _logger.LogDebug("GetCart: Session not valid, returning empty cart");
                return Ok(new List<OrderItemDto>());
            }

            try
            {
                var cartItems = await _orderService.GetCartBySessionId(sessionId.Value);
                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCart: Unexpected error");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ПОЛУЧИТЬ СПИСОК СЛУЖБ ДОСТАВКИ (ПУБЛИЧНЫЙ)
        // ═══════════════════════════════════════════════════════

        [HttpGet("shipCompanies")]
        public async Task<IActionResult> GetShipCompanies()
        {
            try
            {
                var companies = await _orderService.GetAllShipCompanies();
                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetShipCompanies: Unexpected error");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ОФОРМИТЬ ЗАКАЗ (CHECKOUT)
        // ═══════════════════════════════════════════════════════

        [HttpPost("checkout")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, sessionId, error) = await GetValidatedSessionIdAsync();

            if (!success || !sessionId.HasValue)
            {
                _logger.LogWarning("CreateOrder: {Error}", error);
                return Unauthorized(new { error = error });
            }

            try
            {
                _logger.LogInformation("CreateOrder: checkout requested for SessionId={SessionId}", sessionId);

                // ✅ Передаём валидированный SessionId в сервис
                request.SessionId = sessionId.Value;
                var response = await _orderService.CreateOrder(request);

                _logger.LogInformation(
                    "Order created successfully. OrderId={OrderId}, SessionId={SessionId}",
                    response.OrderId, sessionId);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "CreateOrder: Business logic error");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateOrder: Unexpected error");
                return StatusCode(500, new { error = "Ошибка при оформлении заказа." });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ССЫЛКА ОТСЛЕЖИВАНИЯ ДЛЯ ЗАКАЗА ТЕКУЩЕЙ СЕССИИ
        // ═══════════════════════════════════════════════════════

        [HttpGet("{orderId:int}/tracking-link")]
        public async Task<IActionResult> GetTrackingLink(int orderId)
        {
            var (success, sessionId, error) = await GetValidatedSessionIdAsync();
            if (!success || !sessionId.HasValue)
            {
                return Unauthorized(new { error = error });
            }

            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { error = "Заказ не найден" });
                }

                if (order.SessionId != sessionId.Value)
                {
                    return Forbid();
                }

                var token = _trackingTokenService.CreateToken(orderId);
                var trackingPath = $"/order-track/{orderId}?t={Uri.EscapeDataString(token)}";
                return Ok(new { trackingPath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTrackingLink: Unexpected error for OrderId={OrderId}", orderId);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ПУБЛИЧНОЕ ОТСЛЕЖИВАНИЕ ЗАКАЗА (HMAC-токен из письма)
        // ═══════════════════════════════════════════════════════

        [HttpGet("track/{orderId:int}")]
        public async Task<IActionResult> TrackOrder(int orderId, [FromQuery] string? t)
        {
            if (!_trackingTokenService.ValidateToken(orderId, t))
            {
                return NotFound(new { error = "Заказ не найден" });
            }

            try
            {
                var order = await _orderService.GetOrderForTrackingAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { error = "Заказ не найден" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TrackOrder: Unexpected error for OrderId={OrderId}", orderId);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ═══════════════════════════════════════════════════════
        // ПОЛУЧИТЬ ЗАКАЗ ПО ID (ПУБЛИЧНЫЙ ИЛИ С ПРОВЕРКОЙ)
        // ═══════════════════════════════════════════════════════

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var (success, sessionId, error) = await GetValidatedSessionIdAsync();
            if (!success || !sessionId.HasValue)
            {
                return Unauthorized(new { error = error });
            }

            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);

                if (order == null)
                    return NotFound(new { error = "Order not found" });

                if (order.SessionId != sessionId.Value)
                {
                    return Forbid();
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrderById: Unexpected error");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }
    }
}
