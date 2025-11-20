using Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IOrderService
    {
        Task<int> CreateOrder(OrderDto orderDto);
        Task<(int OrderId, int OrderItemId)> AddProductToCart(int productId, int sessionId);
        Task RemoveProductFromCart(int orderItemId);
        Task UpdateOrderItemQuantity(int orderItemId, int quantity);
        Task ClearCart(int sessionId);
        Task<List<CartItemDto>> GetCartBySessionId(int sessionId);
        Task<List<ShipCompanyDto>> GetAllShipCompanies();
    }
}
