using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserSessionController : ControllerBase
    {
        private readonly IUserSessionService _userSessionService;
        private readonly IOrderService _orderService;
        public UserSessionController(IUserSessionService userSessionService, IOrderService orderService)
        {
            _userSessionService = userSessionService;
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] UserSessionDto userSessionDto)
        {
            var sessionId = await _userSessionService.CreateUserSession(userSessionDto);
            OrderDto newOrderDto = new OrderDto
            {
                OrderStatus = "Draft",
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                ShipMethod = "draft",
                PayStatus = "Unpayed",
                CustomerFullname = "draft",
                PayMethod = "draft",
                CustomerEmail = "draft",
                CustomerPhoneNumber = "draft",
                SessionId = sessionId,
            };
            var orderId = await _orderService.CreateOrder(newOrderDto);
            return Ok(new SessionCreationResult
            {
                OrderId = orderId,
                SessionId = sessionId,
                Message = "Сессия успешно создана"
            });
        }
    }
}
