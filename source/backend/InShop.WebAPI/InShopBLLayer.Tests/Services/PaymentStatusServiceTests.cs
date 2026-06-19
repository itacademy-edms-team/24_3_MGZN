using FluentAssertions;
using InShopBLLayer.Services;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Moq;

namespace InShopBLLayer.Tests.Services;

public class PaymentStatusServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly PaymentStatusService _sut;

    public PaymentStatusServiceTests()
    {
        _sut = new PaymentStatusService(_orderRepository.Object);
    }

    [Fact]
    public async Task GetOrderSatusAsync_WhenOrderExists_ReturnsStatus()
    {
        _orderRepository.Setup(r => r.GetOrderById(7))
            .ReturnsAsync(new Order { OrderId = 7, OrderStatus = "Unpayed" });

        var status = await _sut.GetOrderSatusAsync(7);

        status.Should().Be("Unpayed");
    }

    [Fact]
    public async Task GetOrderSatusAsync_WhenOrderMissing_ReturnsNotFound()
    {
        _orderRepository.Setup(r => r.GetOrderById(It.IsAny<int>()))
            .ReturnsAsync((Order?)null!);

        var status = await _sut.GetOrderSatusAsync(404);

        status.Should().Be("NotFound");
    }
}
