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

            _productId = product.ProductId;
            _sessionToken = session.SessionToken;
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

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddToCart_WithValidSession_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/Order", new AddToCartDto { ProductId = _productId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("orderId");
    }

    [Fact]
    public async Task GetCart_AfterAddToCart_ReturnsItems()
    {
        await _client.PostAsJsonAsync("/api/Order", new AddToCartDto { ProductId = _productId });

        var response = await _client.GetAsync("/api/Order/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<CartItemDto>>();
        items.Should().NotBeNull().And.NotBeEmpty();
    }
}
