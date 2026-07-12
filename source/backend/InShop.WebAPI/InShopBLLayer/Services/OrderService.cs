using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Services.Admin;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;

namespace InShopBLLayer.Services
{
    internal class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productReposiotory;
        private readonly IOrderStatusEmailNotifier _emailNotifier;
        private readonly IMapper _mapper;

        public OrderService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IMapper mapper,
            IOrderStatusEmailNotifier emailNotifier)
        {
            _orderRepository = orderRepository;
            _productReposiotory = productRepository;
            _emailNotifier = emailNotifier;
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
                    OrderTotalAmount = 0,
                    // Keep draft creation aligned with DB NOT NULL constraints.
                    ShipMethod = "draft",
                    PayStatus = "Unpayed",
                    CustomerFullname = "draft",
                    PayMethod = "draft",
                    CustomerEmail = "draft",
                    CustomerPhoneNumber = "draft"
                };
                await _orderRepository.CreateOrder(order);
            }

            var product = await _productReposiotory.GetProduct(productId);
            if (product == null)
                throw new InvalidOperationException("Товар не найден");
            if (!product.ProductAvailability)
                throw new InvalidOperationException("Товар недоступен для заказа");

            var existingItem = await _orderRepository.GetOrderItemByOrderIdAndProductId(order.OrderId, productId);

            if(existingItem != null)
            {
                if (existingItem.QuantityItem + 1 > product.ProductStockQuantity)
                    throw new InvalidOperationException("Недостаточно товара на складе");

                existingItem.QuantityItem += 1;
                existingItem.TotalPrice = existingItem.Price * existingItem.QuantityItem;
                await _orderRepository.UpdateOrderItem(existingItem);
                orderItemId = existingItem.OrderItemId;
            }
            else
            {
                if (product.ProductStockQuantity < 1)
                    throw new InvalidOperationException("Недостаточно товара на складе");

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
            if (order == null)
                throw new InvalidOperationException("Заказ не найден");

            order.OrderTotalAmount = totalAmount;
            await _orderRepository.UpdateOrder(order);
        }
        public async Task UpdateOrderItemQuantity(int orderItemId, int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Количество должно быть больше нуля");

            var orderItem = await _orderRepository.GetOrderItemById(orderItemId);

            if (orderItem == null)
                throw new InvalidOperationException("Товар не найден в корзине");

            var product = await _productReposiotory.GetProduct(orderItem.ProductId);
            if (product == null)
                throw new InvalidOperationException("Товар не найден");
            if (!product.ProductAvailability)
                throw new InvalidOperationException("Товар недоступен для заказа");
            if (quantity > product.ProductStockQuantity)
                throw new InvalidOperationException("Недостаточно товара на складе");

            orderItem.QuantityItem = quantity;
            orderItem.TotalPrice = orderItem.Price * quantity;

            await _orderRepository.UpdateOrderItem(orderItem);

            var totalAmount = await _orderRepository.CalculateOrderTotalAmount(orderItem.OrderId);

            var order = await _orderRepository.GetOrderById(orderItem.OrderId);
            if (order == null)
                throw new InvalidOperationException("Заказ не найден");

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
            // Checkout can only finalize the current draft order for this session.
            var existingOrder = await _orderRepository.GetDraftOrderBySessionIdAsync(requestDto.SessionId);

            if (existingOrder != null)
            {
                if (!existingOrder.OrderItems.Any())
                {
                    throw new InvalidOperationException("Нельзя оформить пустую корзину.");
                }

                // Обновляем поля заказа
                existingOrder.ShipCompanyId = requestDto.ShipCompanyId;
                existingOrder.ShipAddress = requestDto.ShipAddress;
                existingOrder.ShipMethod = requestDto.ShipMethod;
                existingOrder.PayMethod = requestDto.PayMethod;
                existingOrder.CustomerFullname = requestDto.CustomerFullname;
                existingOrder.CustomerEmail = requestDto.CustomerEmail;
                existingOrder.CustomerPhoneNumber = requestDto.CustomerPhoneNumber;

                foreach (var item in existingOrder.OrderItems)
                {
                    if (item.QuantityItem <= 0)
                    {
                        throw new InvalidOperationException("Количество товара должно быть больше нуля.");
                    }

                    var product = item.Product ?? await _productReposiotory.GetProduct(item.ProductId);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Товар {item.ProductId} не найден.");
                    }

                    if (!product.ProductAvailability)
                    {
                        throw new InvalidOperationException($"Товар {product.ProductName} недоступен для заказа.");
                    }

                    if (item.QuantityItem > product.ProductStockQuantity)
                    {
                        throw new InvalidOperationException($"Недостаточно товара {product.ProductName} на складе.");
                    }

                    item.Price = product.ProductPrice;
                    item.TotalPrice = item.QuantityItem * item.Price;
                }

                existingOrder.OrderTotalAmount = existingOrder.OrderItems.Sum(oi => oi.Price * oi.QuantityItem);

                // Меняем статус
                existingOrder.OrderStatus = "Unpayed";

                // Обновляем дату
                existingOrder.OrderDate = DateOnly.FromDateTime(DateTime.UtcNow);

                // Сохраняем изменения
                var updatedOrder = await _orderRepository.UpdateOrder(existingOrder);
                // Перезагружаем конкретный оформленный заказ, чтобы получить Product
                var orderForEmail = await _orderRepository.GetOrderById(updatedOrder.OrderId);

                if (orderForEmail == null)
                    throw new InvalidOperationException("Не удалось получить оформленный заказ.");

                await _emailNotifier.SendOrderConfirmationAsync(orderForEmail);

                return _mapper.Map<OrderResponseDto>(orderForEmail);
            }
            else
            {
                throw new InvalidOperationException("Черновик заказа для текущей сессии не найден.");
            }
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);

            if (order == null)
                return null;

            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<OrderTrackDto?> GetOrderForTrackingAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);
            if (order == null)
            {
                return null;
            }

            var canonical = OrderStatusStateMachine.Normalize(order.OrderStatus);
            return new OrderTrackDto
            {
                OrderId = order.OrderId,
                OrderStatus = canonical,
                OrderStatusDisplay = OrderStatusLabels.ToRussian(order.OrderStatus),
                OrderDate = order.OrderDate.ToString("dd.MM.yyyy"),
                OrderTotalAmount = order.OrderTotalAmount,
                ShipMethod = order.ShipMethod,
                ShipAddress = order.ShipAddress,
                CustomerFullname = order.CustomerFullname,
                PayMethod = order.PayMethod,
                PayStatus = order.PayStatus,
                Items = (order.OrderItems ?? Enumerable.Empty<OrderItem>())
                    .Select(i => new OrderTrackItemDto
                    {
                        ProductName = i.Product?.ProductName ?? $"Товар #{i.ProductId}",
                        Quantity = i.QuantityItem,
                        UnitPrice = i.Price,
                        LineTotal = i.TotalPrice ?? i.Price * i.QuantityItem
                    })
                    .ToList()
            };
        }

        public async Task<OrderResponseDto?> GetOrderBySessionIdAsync(int sessionId)
        {
            var order = await _orderRepository.GetOrderBySessionIdAsync(sessionId);

            if (order == null)
                return null;

            return _mapper.Map<OrderResponseDto>(order);
        }
        public async Task<bool> OrderItemBelongsToSessionAsync(int orderItemId, int sessionId)
        {
            var orderItem = await _orderRepository.GetOrderItemById(orderItemId);

            if (orderItem == null)
                return false;

            // Проверяем, что заказ принадлежит сессии
            var order = await _orderRepository.GetOrderById(orderItem.OrderId);
            return order?.SessionId == sessionId;
        }
    }
}
