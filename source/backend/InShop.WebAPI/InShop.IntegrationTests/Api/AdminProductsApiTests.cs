using System.Net;
using System.Net.Http.Json;
using Contracts.Admin.Dto;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class AdminProductsApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int _categoryId;

    public AdminProductsApiTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ClearIdentityUsersAsync();

        _categoryId = await _fixture.WithFreshDatabaseAsync(async context =>
        {
            var (category, _) = await TestDataSeeder.SeedCatalogAsync(context);
            return category.CategoryId;
        });

        _factory = new InShopWebApplicationFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();

        var token = await ApiTestSupport.RegisterAdminAndGetTokenAsync(_client);
        _client.UseBearerToken(token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetProducts_WithoutJwt_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Admin/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        var response = await _client.PostAsJsonAsync("/api/Admin/products", new AdminProductCreateDto
        {
            ProductName = "Admin Created Laptop",
            ProductPrice = 120000m,
            ProductAvailability = true,
            ProductCategoryId = _categoryId,
            ProductStockQuantity = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<AdminProductDto>();
        product.Should().NotBeNull();
        product!.ProductName.Should().Be("Admin Created Laptop");
        product.ProductId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProducts_ReturnsPagedList()
    {
        await _client.PostAsJsonAsync("/api/Admin/products", new AdminProductCreateDto
        {
            ProductName = "Paged Product",
            ProductPrice = 50000m,
            ProductCategoryId = _categoryId,
            ProductStockQuantity = 1
        });

        var response = await _client.GetAsync("/api/Admin/products?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<AdminProductDto>>();
        page.Should().NotBeNull();
        page!.Items.Should().NotBeEmpty();
        page.TotalCount.Should().BeGreaterThan(0);
    }
}
