using System.Net;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class ProductsApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int _productId;

    public ProductsApiTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _productId = await _fixture.WithFreshDatabaseAsync(async context =>
        {
            var (_, product) = await TestDataSeeder.SeedCatalogAsync(
                context,
                productName: "Integration Laptop");
            return product.ProductId;
        });

        _factory = new InShopWebApplicationFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededProduct()
    {
        var response = await _client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Integration Laptop");
    }

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/Products/{_productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_WhenProductMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/Products/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductsByCategory_ReturnsFilteredProducts()
    {
        var response = await _client.GetAsync(
            "/api/Products/products-by-category?categoryName=Ноутбуки&sortBy=ProductName&sortOrder=asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Integration Laptop");
    }

    [Fact]
    public async Task GetProductsByCategory_WhenInvalidSort_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            "/api/Products/products-by-category?categoryName=Ноутбуки&sortBy=InvalidColumn");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
