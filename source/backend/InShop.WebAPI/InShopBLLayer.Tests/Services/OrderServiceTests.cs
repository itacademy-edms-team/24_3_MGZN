using AutoMapper;
using Contracts.Dtos;
using FluentAssertions;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Services;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Moq;

namespace InShopBLLayer.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(
            _orderRepository.Object,
            _productRepository.Object,
            _mapper.Object,
            _emailSender.Object);
    }

    [Fact]
    public async Task AddProductToCart_WhenNoDraft_CreatesDraftOrderAndItem()
    {
        const int sessionId = 42;
        const int productId = 7;
        var product = new Product { ProductId = productId, ProductPrice = 1000m };

        _orderRepository.Setup(r => r.GetDraftOrderBySessionId(sessionId))
            .ReturnsAsync((Order?)null);
        _orderRepository.Setup(r => r.CreateOrder(It.IsAny<Order>()))
            .Callback<Order>(o => o.OrderId = 1)
            .ReturnsAsync((Order o) => o);
        _productRepository.Setup(r => r.GetProduct(productId)).ReturnsAsync(product);
        _orderRepository.Setup(r => r.GetOrderItemByOrderIdAndProductId(1, productId))
            .ReturnsAsync((OrderItem?)null);
        _orderRepository.Setup(r => r.CreateOrderItem(It.IsAny<OrderItem>()))
            .ReturnsAsync(99);
        _orderRepository.Setup(r => r.CalculateOrderTotalAmount(1)).ReturnsAsync(1000m);

        var (orderId, itemId) = await _sut.AddProductToCart(productId, sessionId);

        orderId.Should().Be(1);
        itemId.Should().Be(99);
        _orderRepository.Verify(r => r.CreateOrder(It.Is<Order>(o =>
            o.OrderStatus == "Draft" && o.SessionId == sessionId)), Times.Once);
    }

    [Fact]
    public async Task AddProductToCart_WhenItemExists_IncrementsQuantity()
    {
        const int sessionId = 1;
        const int productId = 5;
        var order = new Order { OrderId = 10, SessionId = sessionId };
        var existingItem = new OrderItem
        {
            OrderItemId = 20,
            OrderId = 10,
            ProductId = productId,
            QuantityItem = 2,
            Price = 500m,
            TotalPrice = 1000m
        };

        _orderRepository.Setup(r => r.GetDraftOrderBySessionId(sessionId)).ReturnsAsync(order);
        _productRepository.Setup(r => r.GetProduct(productId))
            .ReturnsAsync(new Product { ProductId = productId, ProductPrice = 500m });
        _orderRepository.Setup(r => r.GetOrderItemByOrderIdAndProductId(10, productId))
            .ReturnsAsync(existingItem);
        _orderRepository.Setup(r => r.CalculateOrderTotalAmount(10)).ReturnsAsync(1500m);

        var (_, itemId) = await _sut.AddProductToCart(productId, sessionId);

        itemId.Should().Be(20);
        existingItem.QuantityItem.Should().Be(3);
        existingItem.TotalPrice.Should().Be(1500m);
        _orderRepository.Verify(r => r.UpdateOrderItem(existingItem), Times.Once);
    }

    [Fact]
    public async Task AddProductToCart_WhenProductMissing_Throws()
    {
        _orderRepository.Setup(r => r.GetDraftOrderBySessionId(It.IsAny<int>()))
            .ReturnsAsync(new Order { OrderId = 1 });
        _productRepository.Setup(r => r.GetProduct(It.IsAny<int>()))
            .ReturnsAsync((Product?)null);

        var act = () => _sut.AddProductToCart(99, 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Товар не найден*");
    }

    [Fact]
    public async Task RemoveProductFromCart_UpdatesOrderTotal()
    {
        var orderItem = new OrderItem { OrderItemId = 5, OrderId = 3, Price = 100m };
        var order = new Order { OrderId = 3, OrderTotalAmount = 100m };

        _orderRepository.Setup(r => r.GetOrderItemById(5)).ReturnsAsync(orderItem);
        _orderRepository.Setup(r => r.CalculateOrderTotalAmount(3)).ReturnsAsync(0m);
        _orderRepository.Setup(r => r.GetOrderById(3)).ReturnsAsync(order);

        await _sut.RemoveProductFromCart(5);

        _orderRepository.Verify(r => r.DeleteOrderItem(5), Times.Once);
        order.OrderTotalAmount.Should().Be(0m);
        _orderRepository.Verify(r => r.UpdateOrder(order), Times.Once);
    }

    [Fact]
    public async Task RemoveProductFromCart_WhenItemMissing_Throws()
    {
        _orderRepository.Setup(r => r.GetOrderItemById(It.IsAny<int>()))
            .ReturnsAsync((OrderItem?)null);

        var act = () => _sut.RemoveProductFromCart(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*не найден в корзине*");
    }

    [Fact]
    public async Task UpdateOrderItemQuantity_RecalculatesLineAndOrderTotals()
    {
        var orderItem = new OrderItem
        {
            OrderItemId = 8,
            OrderId = 2,
            QuantityItem = 1,
            Price = 200m
        };
        var order = new Order { OrderId = 2 };

        _orderRepository.Setup(r => r.GetOrderItemById(8)).ReturnsAsync(orderItem);
        _orderRepository.Setup(r => r.CalculateOrderTotalAmount(2)).ReturnsAsync(600m);
        _orderRepository.Setup(r => r.GetOrderById(2)).ReturnsAsync(order);

        await _sut.UpdateOrderItemQuantity(8, 3);

        orderItem.QuantityItem.Should().Be(3);
        orderItem.TotalPrice.Should().Be(600m);
        order.OrderTotalAmount.Should().Be(600m);
    }

    [Fact]
    public async Task GetCartBySessionId_WhenNoDraft_ReturnsEmptyList()
    {
        _orderRepository.Setup(r => r.GetDraftOrderBySessionId(1))
            .ReturnsAsync((Order?)null);

        var cart = await _sut.GetCartBySessionId(1);

        cart.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCartBySessionId_MapsItemsToCartDtos()
    {
        var order = new Order { OrderId = 4 };
        var items = new List<OrderItem>
        {
            new()
            {
                OrderItemId = 1,
                ProductId = 10,
                QuantityItem = 2,
                Price = 150m,
                Product = new Product { ProductName = "Наушники", ImageUrl = "/img/a.png" }
            }
        };

        _orderRepository.Setup(r => r.GetDraftOrderBySessionId(5)).ReturnsAsync(order);
        _orderRepository.Setup(r => r.GetOrderItemsByOrderId(4)).ReturnsAsync(items);

        var cart = await _sut.GetCartBySessionId(5);

        cart.Should().ContainSingle();
        cart[0].ProductName.Should().Be("Наушники");
        cart[0].Quantity.Should().Be(2);
        cart[0].ImageUrl.Should().Be("/img/a.png");
    }

    [Fact]
    public async Task CreateOrder_WhenDraftMissing_Throws()
    {
        _orderRepository.Setup(r => r.GetDraftOrderBySessionIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Order?)null);

        var act = () => _sut.CreateOrder(new CreateOrderRequestDto { SessionId = 1 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Черновик заказа*");
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
  public async Task OrderItemBelongsToSessionAsync_ChecksOrderSession(int orderSessionId, int checkSessionId, bool expected)
    {
        var orderItem = new OrderItem { OrderItemId = 1, OrderId = 10 };
        var order = new Order { OrderId = 10, SessionId = orderSessionId };

        _orderRepository.Setup(r => r.GetOrderItemById(1)).ReturnsAsync(orderItem);
        _orderRepository.Setup(r => r.GetOrderById(10)).ReturnsAsync(order);

        var result = await _sut.OrderItemBelongsToSessionAsync(1, checkSessionId);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task OrderItemBelongsToSessionAsync_WhenItemMissing_ReturnsFalse()
    {
        _orderRepository.Setup(r => r.GetOrderItemById(It.IsAny<int>()))
            .ReturnsAsync((OrderItem?)null);

        var result = await _sut.OrderItemBelongsToSessionAsync(1, 1);

        result.Should().BeFalse();
    }
}
