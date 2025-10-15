using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Repositories
{
    internal class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _appDbContext;
        public OrderRepository(AppDbContext context)
        {
            _appDbContext = context;
        }
        public async Task<int> CreateOrder(Order order)
        {
            await _appDbContext.Orders.AddAsync(order);
            await _appDbContext.SaveChangesAsync();
            return order.OrderId;
        }
        public async Task UpdateOrder(Order order)
        {
            _appDbContext.Orders.Update(order);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task<Order> GetDraftOrderBySessionId(int sessionId)
        {
            return await _appDbContext.Orders
                .Where(o => o.SessionId == sessionId && o.OrderStatus == "Draft")
                .FirstOrDefaultAsync();
        }
        public async Task<OrderItem> GetOrderItemByOrderIdAndProductId(int orderId, int productId)
        {
            return await _appDbContext.OrderItems
                .Where(oi => oi.OrderId == orderId && oi.ProductId == productId)
                .FirstOrDefaultAsync();
        }
        public async Task<int> CreateOrderItem(OrderItem orderItem)
        {
            await _appDbContext.OrderItems.AddAsync(orderItem);
            await _appDbContext.SaveChangesAsync();
            return orderItem.OrderItemId;
        }
        public async Task UpdateOrderItem(OrderItem orderItem)
        {
            _appDbContext.OrderItems.Update(orderItem);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task<decimal> CalculateOrderTotalAmount(int orderId)
        {
            return (decimal)await _appDbContext.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .SumAsync(oi => oi.TotalPrice);
        }
        public async Task<OrderItem> GetOrderItemById(int orderItemId)
        {
            return await _appDbContext.OrderItems.FindAsync(orderItemId);
        }
        public async Task DeleteOrderItem(int orderItemId)
        {
            var item = await GetOrderItemById(orderItemId);
            if (item != null)
            {
                _appDbContext.OrderItems.Remove(item);
                await _appDbContext.SaveChangesAsync();
            }
        }
        public async Task<Order> GetOrderById(int orderId)
        {
            return await _appDbContext.Orders.FindAsync(orderId);
        }
        public async Task DeleteAllOrderItems(int orderId)
        {
            var items = await _appDbContext.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
            _appDbContext.OrderItems.RemoveRange(items);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task<List<OrderItem>> GetOrderItemsByOrderId(int orderId)
        {
            return await _appDbContext.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Include(oi => oi.Product)
                .ToListAsync();
        }
    }
}
