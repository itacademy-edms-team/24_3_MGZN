using System.Net;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;
using InShopDbModels.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class CategoryApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int _seededCategoryId;

    public CategoryApiTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _seededCategoryId = await _fixture.WithFreshDatabaseAsync(async context =>
        {
            var category = new Category { CategoryName = "Ноутбуки" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category.CategoryId;
        });

        _factory = new InShopWebApplicationFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededCategories()
    {
        var response = await _client.GetAsync("/api/Category");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Ноутбуки");
    }

    [Fact]
    public async Task GetById_WhenCategoryExists_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/Category/{_seededCategoryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

