using System.Net;
using System.Net.Http.Json;
using Contracts.Dtos;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class UserSessionApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public UserSessionApiTests(SqlServerFixture fixture)
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
    public async Task CreateSession_ReturnsSessionWithOrderId()
    {
        var response = await _client.PostAsJsonAsync("/api/UserSession", new UserSessionDto
        {
            UserIpaddress = "127.0.0.1",
            UserAgent = "integration-test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionCreationResult>();
        result.Should().NotBeNull();
        result!.SessionId.Should().BeGreaterThan(0);
        result.OrderId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateSession_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/UserSession/validate");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
