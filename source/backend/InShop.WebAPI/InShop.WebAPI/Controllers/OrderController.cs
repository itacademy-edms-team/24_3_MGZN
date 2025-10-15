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
    }
}
