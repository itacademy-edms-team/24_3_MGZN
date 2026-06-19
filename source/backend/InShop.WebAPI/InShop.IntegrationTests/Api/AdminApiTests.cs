using System.Net;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class AdminApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public AdminApiTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _factory = new InShopWebApplicationFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetOrders_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
