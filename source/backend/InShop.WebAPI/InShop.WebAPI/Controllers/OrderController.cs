using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Contracts.Dtos;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService service)
        {
            _orderService = service;
        }
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var (orderId, orderItemId) = await _orderService.AddProductToCart(dto.ProductId, dto.SessionId);
                return Ok(new { orderId, orderItemId, message = "Товар успешно добавлен в корзину." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("{orderItemId}")]
        public async Task<IActionResult> RemoveFromCart(int orderItemId)
        {
            try
            {
                await _orderService.RemoveProductFromCart(orderItemId);
                return Ok(new {message = "Товар успешно удалён из корзины."});
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("updateQuantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityDto dto)
        {
            try
            {
                await _orderService.UpdateOrderItemQuantity(dto.OrderItemId, dto.Quantity);
                return Ok(new { message = "Количество успешно обновлено." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart([FromQuery] int sessionId)
        {
            try
            {
                await _orderService.ClearCart(sessionId);
                return Ok(new {message = "Корзина очищена."});
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart([FromQuery] int sessionId)
        {
            try
            {
                var cartItems = await _orderService.GetCartBySessionId(sessionId);
                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("shipCompanies")]
        public async Task<IActionResult> GetShipCompanies()
        {
            try
            {
                var companies = await _orderService.GetAllShipCompanies();
                return Ok(companies);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("checkout")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                Console.WriteLine($"Получено OrderItems: {request.OrderItems.Count}");
                foreach (var item in request.OrderItems)
                {
                    Console.WriteLine($"  ProductId: {item.ProductId}, QuantityItem: {item.QuantityItem}, Price: {item.Price}");
                }
                var response = await _orderService.CreateOrder(request);
                return Ok(response); // Не CreatedAtAction, т.к. мы обновляем существующий заказ
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ошибка при оформлении заказа.", details = ex.Message });
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}
