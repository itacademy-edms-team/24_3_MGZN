using Azure.Core;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;
using InShop.WebAPI.Services;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentProcessingService _paymentProcessingService;

        public PaymentController(PaymentProcessingService paymentProcessingService)
        {
            _paymentProcessingService = paymentProcessingService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestDto request)
        {
            if (request.OrderId <= 0)
            {
                return BadRequest(new { message = "Invalid OrderId." });
            }

            using var scope = HttpContext.RequestServices.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<InShopDbModels.Abstractions.IOrderRepository>();
            var order = await orderRepository.GetOrderById(request.OrderId);
            if (order == null)
            {
                return BadRequest(new { message = "Order not found." });
            }

            if (order.OrderStatus != "Unpayed")
            {
                return BadRequest(new { message = "Order is not in 'Unpayed' status." });
            }

            await _paymentProcessingService.InitiatePaymentAsync(request);

            return Ok(new { message = "Payment process initiated successfully." });
        }

        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetPaymentStatus(int orderId)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var paymentStatusService = scope.ServiceProvider.GetRequiredService<IPaymentStatusService>();

            var status = await paymentStatusService.GetOrderSatusAsync(orderId);

            if (status == "NotFound")
            {
                return NotFound(new { error = "Order not found" });
            }

            return Ok(new { Status = status });
        }
    }
}
