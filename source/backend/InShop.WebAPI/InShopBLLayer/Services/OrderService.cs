using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using InShopDbModels.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    internal class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderReposiotory;
        private readonly IProductRepository _productReposiotory;
        private readonly IMapper _mapper;
        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IMapper mapper)
        {
            _orderReposiotory = orderRepository;
            _productReposiotory = productRepository;
            _mapper = mapper;
        }
        public async Task<int> CreateOrder(OrderDto orderDto)
        {
            var order = _mapper.Map<Order>(orderDto);
            return await _orderReposiotory.CreateOrder(order);
        }
        public async Task<(int OrderId, int OrderItemId)> AddProductToCart(int productId, int sessionId)
        {
            var order = await _orderReposiotory.GetDraftOrderBySessionId(sessionId);
            int orderItemId;

            if (order == null)
            {
                order = new Order
                {
                    OrderStatus = "Draft",
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    SessionId = sessionId,
                    OrderTotalAmount = 0
                };
                await _orderReposiotory.CreateOrder(order);
            }

            var product = await _productReposiotory.GetProduct(productId);
            if (product == null)
                throw new InvalidOperationException("Товар не найден");

            var existingItem = await _orderReposiotory.GetOrderItemByOrderIdAndProductId(order.OrderId, productId);

            if(existingItem != null)
            {
                existingItem.QuantityItem += 1;
                existingItem.TotalPrice = existingItem.Price * existingItem.QuantityItem;
                await _orderReposiotory.UpdateOrderItem(existingItem);
                orderItemId = existingItem.OrderItemId;
            }
            else
            {
                var newItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = productId,
                    QuantityItem = 1,
                    Price = product.ProductPrice,
                    TotalPrice = product.ProductPrice
                };
                orderItemId = await _orderReposiotory.CreateOrderItem(newItem);
            }

            var totalAmount = await _orderReposiotory.CalculateOrderTotalAmount(order.OrderId);
            order.OrderTotalAmount = totalAmount;
            await _orderReposiotory.UpdateOrder(order);

            return (order.OrderId, orderItemId);
        }
        public async Task RemoveProductFromCart(int orderItemId)
        {
            var orderItem = await _orderReposiotory.GetOrderItemById(orderItemId);

            if(orderItem == null)
                throw new InvalidOperationException("Товар не найден в корзине");

            await _orderReposiotory.DeleteOrderItem(orderItemId);

            var totalAmount = await _orderReposiotory.CalculateOrderTotalAmount(orderItem.OrderId);

            var order = await _orderReposiotory.GetOrderById(orderItem.OrderId);
            order.OrderTotalAmount = totalAmount;
            await _orderReposiotory.UpdateOrder(order);
        }
        public async Task UpdateOrderItemQuantity(int orderItemId, int quantity)
        {
            var orderItem = await _orderReposiotory.GetOrderItemById(orderItemId);

            if (orderItem == null)
                throw new InvalidOperationException("Товар не найден в корзине");

            orderItem.QuantityItem = quantity;
            orderItem.TotalPrice = orderItem.Price * quantity;

            await _orderReposiotory.UpdateOrderItem(orderItem);

            var totalAmount = await _orderReposiotory.CalculateOrderTotalAmount(orderItem.OrderId);

            var order = await _orderReposiotory.GetOrderById(orderItem.OrderId);
            order.OrderTotalAmount = totalAmount;
            await _orderReposiotory.UpdateOrder(order);
        }
        public async Task ClearCart(int sessionId)
        {
            var order = await _orderReposiotory.GetDraftOrderBySessionId(sessionId);

            if (order == null)
                throw new InvalidOperationException("Заказ не найден");
            await _orderReposiotory.DeleteAllOrderItems(order.OrderId);
        }
        public async Task<List<CartItemDto>> GetCartBySessionId(int sessionId)
        {
            var order = await _orderReposiotory.GetDraftOrderBySessionId(sessionId);

            if (order == null)
                return new List<CartItemDto>();

            var orderItems = await _orderReposiotory.GetOrderItemsByOrderId(order.OrderId);

            var cartItems = orderItems.Select(item => new CartItemDto
            {
                OrderItemId = item.OrderItemId,
                ProductId = item.ProductId,
                ProductName = item.Product?.ProductName ?? "Неизвестный товар",
                ProductPrice = item.Price,
                Quantity = item.QuantityItem,
                ImageUrl = item.Product?.ImageUrl ?? "/images/placeholder.svg"
            }).ToList();

            return cartItems;
        }
        public async Task<List<ShipCompanyDto>> GetAllShipCompanies()
        {
            var companies = await _orderReposiotory.GetAllShipCompanies();
            return companies.Select(c => new ShipCompanyDto
            {
                ShipCompanyId = c.ShipCompanyId,
                ShipCompanyName = c.ShipCompanyName,
                Contact = c.Contact
            }).ToList();
        }
    }
}
