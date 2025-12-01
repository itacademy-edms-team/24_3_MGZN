using AutoMapper;
using Azure.Core;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Models;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using InShopDbModels.Repositories;
using RazorLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace InShopBLLayer.Services
{
    internal class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productReposiotory;
        private readonly IEmailSender _emailSender;
        private readonly IMapper _mapper;
        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IMapper mapper, IEmailSender emailSender)
        {
            _orderRepository = orderRepository;
            _productReposiotory = productRepository;
            _emailSender = emailSender;
            _mapper = mapper;
        }
        public async Task<int> CreateNewOrder(OrderDto orderDto)
        {
            var order = _mapper.Map<Order>(orderDto);
            return await _orderRepository.CreateNewOrder(order);
        }
        public async Task<(int OrderId, int OrderItemId)> AddProductToCart(int productId, int sessionId)
        {
            var order = await _orderRepository.GetDraftOrderBySessionId(sessionId);
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
                await _orderRepository.CreateOrder(order);
            }

            var product = await _productReposiotory.GetProduct(productId);
            if (product == null)
                throw new InvalidOperationException("Товар не найден");

            var existingItem = await _orderRepository.GetOrderItemByOrderIdAndProductId(order.OrderId, productId);

            if(existingItem != null)
            {
                existingItem.QuantityItem += 1;
                existingItem.TotalPrice = existingItem.Price * existingItem.QuantityItem;
                await _orderRepository.UpdateOrderItem(existingItem);
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
                orderItemId = await _orderRepository.CreateOrderItem(newItem);
            }

            var totalAmount = await _orderRepository.CalculateOrderTotalAmount(order.OrderId);
            order.OrderTotalAmount = totalAmount;
            await _orderRepository.UpdateOrder(order);

            return (order.OrderId, orderItemId);
        }
        public async Task RemoveProductFromCart(int orderItemId)
        {
            var orderItem = await _orderRepository.GetOrderItemById(orderItemId);

            if(orderItem == null)
                throw new InvalidOperationException("Товар не найден в корзине");

            await _orderRepository.DeleteOrderItem(orderItemId);

            var totalAmount = await _orderRepository.CalculateOrderTotalAmount(orderItem.OrderId);

            var order = await _orderRepository.GetOrderById(orderItem.OrderId);
            order.OrderTotalAmount = totalAmount;
            await _orderRepository.UpdateOrder(order);
        }
        public async Task UpdateOrderItemQuantity(int orderItemId, int quantity)
        {
            var orderItem = await _orderRepository.GetOrderItemById(orderItemId);

            if (orderItem == null)
                throw new InvalidOperationException("Товар не найден в корзине");

            orderItem.QuantityItem = quantity;
            orderItem.TotalPrice = orderItem.Price * quantity;

            await _orderRepository.UpdateOrderItem(orderItem);

            var totalAmount = await _orderRepository.CalculateOrderTotalAmount(orderItem.OrderId);

            var order = await _orderRepository.GetOrderById(orderItem.OrderId);
            order.OrderTotalAmount = totalAmount;
            await _orderRepository.UpdateOrder(order);
        }
        public async Task ClearCart(int sessionId)
        {
            var order = await _orderRepository.GetDraftOrderBySessionId(sessionId);

            if (order == null)
                throw new InvalidOperationException("Заказ не найден");
            await _orderRepository.DeleteAllOrderItems(order.OrderId);
        }
        public async Task<List<CartItemDto>> GetCartBySessionId(int sessionId)
        {
            var order = await _orderRepository.GetDraftOrderBySessionId(sessionId);

            if (order == null)
                return new List<CartItemDto>();

            var orderItems = await _orderRepository.GetOrderItemsByOrderId(order.OrderId);

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
            var companies = await _orderRepository.GetAllShipCompanies();
            return companies.Select(c => new ShipCompanyDto
            {
                ShipCompanyId = c.ShipCompanyId,
                ShipCompanyName = c.ShipCompanyName,
                Contact = c.Contact
            }).ToList();
        }

        public async Task<OrderResponseDto> CreateOrder(CreateOrderRequestDto requestDto)
        {
            // Найти заказ по SessionId
            var existingOrder = await _orderRepository.GetOrderBySessionIdAsync(requestDto.SessionId);

            if (existingOrder != null)
            {
                if (existingOrder.OrderStatus == "Draft")
                {
                    // Обновляем поля заказа
                    existingOrder.ShipCompanyId = requestDto.ShipCompanyId;
                    existingOrder.ShipAddress = requestDto.ShipAddress;
                    existingOrder.ShipMethod = requestDto.ShipMethod;
                    existingOrder.PayMethod = requestDto.PayMethod;
                    existingOrder.CustomerFullname = requestDto.CustomerFullname;
                    existingOrder.CustomerEmail = requestDto.CustomerEmail;
                    existingOrder.CustomerPhoneNumber = requestDto.CustomerPhoneNumber;

                    // Обновляем OrderItems, если нужно (например, заменить)
                    existingOrder.OrderItems.Clear();
                    foreach (var item in requestDto.OrderItems)
                    {
                        existingOrder.OrderItems.Add(new OrderItem
                        {
                            ProductId = item.ProductId,
                            QuantityItem = item.QuantityItem,
                            Price = item.Price,
                            TotalPrice = item.QuantityItem * item.Price
                        });
                    }

                    // Пересчитываем итоговую сумму
                    existingOrder.OrderTotalAmount = existingOrder.OrderItems.Sum(oi => oi.TotalPrice).GetValueOrDefault();

                    // Меняем статус
                    existingOrder.OrderStatus = "Unpayed";

                    // Обновляем дату
                    existingOrder.OrderDate = DateOnly.FromDateTime(DateTime.UtcNow);

                    // Сохраняем изменения
                    var updatedOrder = await _orderRepository.UpdateOrder(existingOrder);
                    // Перезагрузите заказ, чтобы получить Product
                    var orderForEmail = await _orderRepository.GetOrderBySessionIdAsync(requestDto.SessionId);

                    // Отправляем письмо
                    await SendOrderConfirmationEmailAsync(orderForEmail, orderForEmail.CustomerEmail);

                    return _mapper.Map<OrderResponseDto>(orderForEmail);
                }
                else
                {
                    throw new InvalidOperationException("Заказ не в статусе Draft, оформление невозможно.");
                }
            }
            else
            {
                throw new InvalidOperationException("Заказ с указанным SessionId не найден.");
            }
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);

            if (order == null)
                return null;

            return _mapper.Map<OrderResponseDto>(order);
        }
        private async Task SendOrderConfirmationEmailAsync(Order order, string customerEmail)
        {
            var subject = "Подтверждение заказа";
            var body = GenerateOrderConfirmationHtml(order);

            // Проверки
            if (_emailSender == null)
                throw new InvalidOperationException("_emailSender не был внедрён");

            if (string.IsNullOrEmpty(customerEmail))
                throw new InvalidOperationException("Email получателя пуст");

            if (string.IsNullOrEmpty(body))
                throw new InvalidOperationException("Тело письма пусто");

            try
            {
                await _emailSender.SendAsync(customerEmail, subject, body);
                Console.WriteLine("=== ТЕЛО ПИСЬМА ===");
                Console.WriteLine(body);
                Console.WriteLine("=== КОНЕЦ ТЕЛА ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки письма: {ex.Message}");
            }
        }

        private string GenerateOrderConfirmationHtml(Order order)
        {
            try
            {
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates");

                var engine = new RazorLightEngineBuilder()
                    .UseFileSystemProject(templatePath) // Папка с шаблонами
                    .UseMemoryCachingProvider()
                    .Build();

                var model = new OrderConfirmationTemplateModel
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate.ToString(),
                    OrderStatus = order.OrderStatus,
                    OrderTotalAmount = order.OrderTotalAmount.ToString("C"),
                    OrderItems = order.OrderItems.Select(item => new OrderItemTemplateModel
                    {
                        ProductName = item.Product?.ProductName ?? "Товар не найден",
                        QuantityItem = item.QuantityItem,
                        Price = item.Price.ToString("C"),
                        TotalPrice = item.TotalPrice.ToString(),
                    }).ToList()
                };

                var html = engine.CompileRenderAsync("OrderConfirmationTemplate.cshtml", model).Result;

                return html;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации письма: {ex.Message}");
                return "<p>Ошибка генерации письма.</p>";
            }
        }
        public async Task<OrderResponseDto?> GetOrderBySessionIdAsync(int sessionId)
        {
            var order = await _orderRepository.GetOrderBySessionIdAsync(sessionId);

            if (order == null)
                return null;

            return _mapper.Map<OrderResponseDto>(order);
        }
    }
}
