using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class PaymentStatusService : IPaymentStatusService
    {
        private readonly IOrderRepository _orderRepository;

        public PaymentStatusService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<string> GetOrderSatusAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);
            if (order == null)
            {
                return "NotFound";
            }

            return order.OrderStatus;
        }
    }
}
