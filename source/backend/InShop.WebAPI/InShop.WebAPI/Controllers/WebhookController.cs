using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhookController : ControllerBase
    {
        private readonly InShopDbModels.Abstractions.IOrderRepository _orderRepository;

        public WebhookController(InShopDbModels.Abstractions.IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public class PaymentConfirmationWebhookDto
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        [HttpPost("payment-confirmation")]
        public async Task<IActionResult> HandlePaymentConfirmation([FromBody] PaymentConfirmationWebhookDto dto)
        {
            Console.WriteLine($"[WebhookController] Received webhook for OrderId: {dto.OrderId}, Status: {dto.Status}");

            var order = await _orderRepository.GetOrderById(dto.OrderId);

            if (order != null)
            {
                if (order.OrderStatus == "Unpayed")
                {
                    order.OrderStatus = dto.Status;
                    await _orderRepository.UpdateOrder(order);
                    Console.WriteLine($"[WebhookController] Order status updated to '{dto.Status}' for OrderId: {dto.OrderId}");
                }
                else
                {
                    Console.WriteLine($"[WebhookController] Order {dto.OrderId} status is not 'Unpayed'. Current status: {order.OrderStatus}. Ignoring webhook.");
                }
            }
            else
            {
                Console.WriteLine($"[WebhookController] Order not found for OrderId: {dto.OrderId}");
            }

            return Ok();
        }
    }
}
