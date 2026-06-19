using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Services.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InShop.IntegrationTests.Services;

[Collection("SqlServer")]
public class AdminOrderServiceTests
{
    private readonly SqlServerFixture _fixture;

    public AdminOrderServiceTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ChangeOrderStatusAsync_UpdatesStatusAndWritesAuditLog()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var session = await TestDataSeeder.SeedSessionAsync(context);
        var order = await TestDataSeeder.SeedOrderAsync(context, session.SessionId, status: "Unpayed");

        var mapper = TestMapperFactory.CreateAdminMapper();
        var inventoryMock = new Mock<IInventoryReservationService>();
        var sut = new AdminOrderService(
            context,
            mapper,
            inventoryMock.Object,
            NullLogger<AdminOrderService>.Instance);

        var result = await sut.ChangeOrderStatusAsync(
            order.OrderId,
            OrderStatusStateMachine.Processing,
            "admin@test.local");

        result.OrderStatus.Should().Be(OrderStatusStateMachine.Processing);
        context.OrderAuditLogs.Should().ContainSingle(a =>
            a.OrderId == order.OrderId &&
            a.OldStatus == "Unpayed" &&
            a.NewStatus == OrderStatusStateMachine.Processing &&
            a.ChangedBy == "admin@test.local");
    }

    [Fact]
    public async Task ChangeOrderStatusAsync_FromTerminalStatus_Throws()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var session = await TestDataSeeder.SeedSessionAsync(context);
        var order = await TestDataSeeder.SeedOrderAsync(context, session.SessionId, status: "Delivered");

        var sut = new AdminOrderService(
            context,
            TestMapperFactory.CreateAdminMapper(),
            Mock.Of<IInventoryReservationService>(),
            NullLogger<AdminOrderService>.Instance);

        var act = () => sut.ChangeOrderStatusAsync(order.OrderId, "Processing", "admin@test.local");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*завершённых или отменённых*");
    }

    [Fact]
    public async Task GetOrdersAsync_ExcludesDraftOrdersByDefault()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var session = await TestDataSeeder.SeedSessionAsync(context);
        await TestDataSeeder.SeedOrderAsync(context, session.SessionId, status: "Draft");
        await TestDataSeeder.SeedOrderAsync(context, session.SessionId, status: "Unpayed");

        var sut = new AdminOrderService(
            context,
            TestMapperFactory.CreateAdminMapper(),
            Mock.Of<IInventoryReservationService>(),
            NullLogger<AdminOrderService>.Instance);

        var page = await sut.GetOrdersAsync(page: 1, pageSize: 20, statusFilter: null);

        page.Items.Should().ContainSingle();
        page.Items[0].OrderStatus.Should().Be(OrderStatusStateMachine.Unpaid);
    }
}
