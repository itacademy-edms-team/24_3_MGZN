using FluentAssertions;
using InShopBLLayer.Services.Admin;

namespace InShopBLLayer.Tests.Services.Admin;

public class OrderStatusStateMachineTests
{
    [Theory]
    [InlineData(null, OrderStatusStateMachine.Draft)]
    [InlineData("", OrderStatusStateMachine.Draft)]
    [InlineData("Unpayed", OrderStatusStateMachine.Unpaid)]
    [InlineData("Payed", OrderStatusStateMachine.Paid)]
    [InlineData("Formalization", OrderStatusStateMachine.Processing)]
    [InlineData("Доставлен", OrderStatusStateMachine.Delivered)]
    [InlineData("Processing", OrderStatusStateMachine.Processing)]
    public void Normalize_MapsKnownStatuses(string? input, string expected)
    {
        OrderStatusStateMachine.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(OrderStatusStateMachine.Unpaid, OrderStatusStateMachine.Processing, true)]
    [InlineData("Unpayed", "Processing", true)]
    [InlineData(OrderStatusStateMachine.Unpaid, OrderStatusStateMachine.Delivered, false)]
    [InlineData(OrderStatusStateMachine.Delivered, OrderStatusStateMachine.Cancelled, false)]
    [InlineData(OrderStatusStateMachine.Shipped, OrderStatusStateMachine.Delivered, true)]
    [InlineData(OrderStatusStateMachine.Paid, OrderStatusStateMachine.Cancelled, true)]
    public void CanTransition_ReturnsExpected(string from, string to, bool expected)
    {
        OrderStatusStateMachine.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(OrderStatusStateMachine.Delivered, true)]
    [InlineData(OrderStatusStateMachine.Cancelled, true)]
    [InlineData("Доставлен", true)]
    [InlineData(OrderStatusStateMachine.Processing, false)]
    public void IsTerminalStatus_DetectsFinalStates(string status, bool expected)
    {
        OrderStatusStateMachine.IsTerminalStatus(status).Should().Be(expected);
    }

    [Fact]
    public void GetAllowedNextStatuses_FromUnpaid_IncludesProcessingAndCancelled()
    {
        var next = OrderStatusStateMachine.GetAllowedNextStatuses("Unpayed");

        next.Should().Contain(OrderStatusStateMachine.Processing);
        next.Should().Contain(OrderStatusStateMachine.Cancelled);
    }

    [Fact]
    public void GetAllowedNextStatuses_FromDelivered_IsEmpty()
    {
        OrderStatusStateMachine.GetAllowedNextStatuses(OrderStatusStateMachine.Delivered)
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateTransition_FromUnpaidToProcessing_DoesNotThrow()
    {
        var act = () => OrderStatusStateMachine.ValidateTransition("Unpayed", "Processing");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTransition_FromDeliveredToCancelled_Throws()
    {
        var act = () => OrderStatusStateMachine.ValidateTransition("Delivered", "Cancelled");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*запрещён*");
    }

    [Fact]
    public void ValidateTransition_ToUnknownStatus_Throws()
    {
        var act = () => OrderStatusStateMachine.ValidateTransition("Unpayed", "UnknownStatus");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Неизвестный целевой статус*");
    }
}
