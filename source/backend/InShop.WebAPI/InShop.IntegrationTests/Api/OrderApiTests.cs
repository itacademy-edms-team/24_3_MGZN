using System.Net;
using System.Net.Http.Json;
using Contracts.Dtos;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class OrderApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int _productId;
    private Guid _sessionToken;
    private int _otherOrderId;

    public OrderApiTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.WithFreshDatabaseAsync(async context =>
        {
            var (_, product) = await TestDataSeeder.SeedCatalogAsync(context);
            var session = await TestDataSeeder.SeedSessionAsync(context);
            var otherSession = await TestDataSeeder.SeedSessionAsync(context);
            var otherOrder = await TestDataSeeder.SeedOrderAsync(context, otherSession.SessionId, "Unpayed");

            _productId = product.ProductId;
            _sessionToken = session.SessionToken;
            _otherOrderId = otherOrder.OrderId;
            return 0;
        });

        _factory = new InShopWebApplicationFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.UseSession(_sessionToken);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task AddToCart_WithoutSession_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Order", new AddToCartDto { ProductId = _productId });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, body);
    }

    [Fact]
    public async Task AddToCart_WithValidSession_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/Order", new AddToCartDto { ProductId = _productId });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().Contain("orderId");
    }

    [Fact]
    public async Task GetCart_AfterAddToCart_ReturnsItems()
    {
        var addResponse = await _client.PostAsJsonAsync("/api/Order", new AddToCartDto { ProductId = _productId });
        var addBody = await addResponse.Content.ReadAsStringAsync();
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK, addBody);

        var response = await _client.GetAsync("/api/Order/cart");

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var items = await response.Content.ReadFromJsonAsync<List<CartItemDto>>();
        items.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GetOrderById_WhenOrderBelongsToAnotherSession_ReturnsForbidden()
    {
        var response = await _client.GetAsync($"/api/Order/{_otherOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
