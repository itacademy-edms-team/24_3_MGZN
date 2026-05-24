using Contracts.Admin.Dto;
using InShop.WebAPI.Extensions;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InShop.WebAPI.Controllers.Admin
{
    [ApiController]
    [Route("api/Admin")]
    [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IAdminOrderService _orderService;

        public AdminOrdersController(IAdminOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("orders")]
        public async Task<ActionResult<PagedResultDto<AdminOrderDto>>> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            CancellationToken ct = default)
        {
            var result = await _orderService.GetOrdersAsync(page, pageSize, status, ct);
            return Ok(result);
        }

        [HttpGet("orders/draft")]
        public async Task<ActionResult<PagedResultDto<AdminOrderDto>>> GetDraftOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _orderService.GetDraftOrdersAsync(page, pageSize, ct);
            return Ok(result);
        }

        [HttpGet("orders/{id:int}")]
        public async Task<ActionResult<AdminOrderDetailDto>> GetOrderDetails(int id, CancellationToken ct)
        {
            var details = await _orderService.GetOrderDetailsAsync(id, ct);
            return details is null ? NotFound() : Ok(details);
        }

        [HttpGet("orders/{id:int}/allowed-statuses")]
        public async Task<ActionResult<IReadOnlyList<string>>> GetAllowedStatuses(int id, CancellationToken ct)
        {
            var order = await _orderService.GetOrderByIdAsync(id, ct);
            if (order is null)
            {
                return NotFound();
            }

            return Ok(_orderService.GetAllowedNextStatuses(order.RawOrderStatus ?? order.OrderStatus));
        }

        [HttpPut("orders/{id:int}/status")]
        public async Task<ActionResult<AdminOrderDto>> ChangeStatus(
            int id,
            [FromBody] ChangeOrderStatusDto dto,
            CancellationToken ct)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? "unknown@admin";

            try
            {
                var updated = await _orderService.ChangeOrderStatusAsync(id, dto.NewStatus, email, ct);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
