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
        //CRUD
        Task<IEnumerable<Order>> GetOrders();
        Task<Order?> GetOrder(int id);
        Task CreateOrder(Order order);
        Task DeleteOrder(int id);
        Task UpdateOrder(Order order);
    }
}
