using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IOrderItemRepository
    {
        //CRUD
        Task<IEnumerable<OrderItem>> GetOrderItems();
        Task<OrderItem> GetOrderItem(int id);
        Task AddOrderItem(OrderItem newOrderItem);
        Task DeleteOrderItem(int id);
        Task UpdateOrderItem(OrderItem OrderItem);
    }
}
