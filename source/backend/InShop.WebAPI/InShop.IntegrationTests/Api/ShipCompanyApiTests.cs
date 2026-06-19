using System.Net;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class ShipCompanyApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int _companyId;

    public ShipCompanyApiTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _companyId = await _fixture.WithFreshDatabaseAsync(async context =>
        {
            var company = await TestDataSeeder.SeedShipCompanyAsync(context, "CDEK");
            return company.ShipCompanyId;
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
    public async Task GetAll_ReturnsSeededCompany()
    {
        var response = await _client.GetAsync("/api/ShipCompany");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("CDEK");
    }

    [Fact]
    public async Task GetById_WhenCompanyExists_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/ShipCompany/{_companyId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
