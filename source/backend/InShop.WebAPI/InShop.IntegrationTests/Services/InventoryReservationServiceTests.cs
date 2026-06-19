using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;
using InShopBLLayer.Services.Admin;
using InShopDbModels.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace InShop.IntegrationTests.Services;

[Collection("SqlServer")]
public class InventoryReservationServiceTests
{
    private readonly SqlServerFixture _fixture;

    public InventoryReservationServiceTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReserveAsync_MovesQuantityFromFreePoolToReserved()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var product = await TestDataSeeder.SeedProductAsync(context, stock: 10, reserved: 0);
        var sut = new InventoryReservationService(context, NullLogger<InventoryReservationService>.Instance);

        await sut.ReserveAsync(product.ProductId, 3);

        var updated = await context.Products.FindAsync(product.ProductId);
        updated!.ProductStockQuantity.Should().Be(7);
        updated.ReservedQuantity.Should().Be(3);
    }

    [Fact]
    public async Task ReserveAsync_WhenInsufficientStock_Throws()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var product = await TestDataSeeder.SeedProductAsync(context, stock: 2);
        var sut = new InventoryReservationService(context, NullLogger<InventoryReservationService>.Instance);

        var act = () => sut.ReserveAsync(product.ProductId, 5);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Недостаточно свободного остатка*");
    }

    [Fact]
    public async Task ReleaseAsync_ReturnsQuantityToFreePool()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var product = await TestDataSeeder.SeedProductAsync(context, stock: 5, reserved: 5);
        var sut = new InventoryReservationService(context, NullLogger<InventoryReservationService>.Instance);

        await sut.ReleaseAsync(product.ProductId, 2);

        var updated = await context.Products.FindAsync(product.ProductId);
        updated!.ProductStockQuantity.Should().Be(7);
        updated.ReservedQuantity.Should().Be(3);
    }

    [Fact]
    public async Task FinalizeAsync_ReducesReservedWithoutIncreasingFreePool()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var product = await TestDataSeeder.SeedProductAsync(context, stock: 0, reserved: 4);
        var sut = new InventoryReservationService(context, NullLogger<InventoryReservationService>.Instance);

        await sut.FinalizeAsync(product.ProductId, 2);

        var updated = await context.Products.FindAsync(product.ProductId);
        updated!.ProductStockQuantity.Should().Be(0);
        updated.ReservedQuantity.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReserveAsync_WhenQuantityInvalid_Throws(int quantity)
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var product = await TestDataSeeder.SeedProductAsync(context);
        var sut = new InventoryReservationService(context, NullLogger<InventoryReservationService>.Instance);

        var act = () => sut.ReserveAsync(product.ProductId, quantity);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
