using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IOrderRepository
    {
        Task<int> CreateNewOrder(Order order);
        Task<Order> GetDraftOrderBySessionId(int sessionId);
        Task<OrderItem> GetOrderItemByOrderIdAndProductId(int orderId, int productId);
        Task<int> CreateOrderItem(OrderItem orderItem);
        Task UpdateOrderItem(OrderItem orderItem);
        Task<Order> UpdateOrder(Order order);
        Task<decimal> CalculateOrderTotalAmount(int orderId);
        Task<OrderItem> GetOrderItemById(int orderId);
        Task DeleteOrderItem(int orderItemId);
        Task<Order> GetOrderById(int orderId);
        Task DeleteAllOrderItems(int orderId);
        Task<List<OrderItem>> GetOrderItemsByOrderId(int orderId);
        Task<List<ShipCompany>> GetAllShipCompanies();
        Task<Order?> GetOrderBySessionIdAsync(int sessionId);
        Task<Order> CreateOrder(Order order);
    }
}
