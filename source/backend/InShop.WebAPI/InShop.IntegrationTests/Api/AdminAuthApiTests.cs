using System.Net;
using System.Net.Http.Json;
using Contracts.Admin.Dto;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;

namespace InShop.IntegrationTests.Api;

[Collection("SqlServer")]
public class AdminAuthApiTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private InShopWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public AdminAuthApiTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearIdentityUsersAsync();

        _factory = new InShopWebApplicationFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_CreatesFirstAdminAndReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/Admin/auth/register", new AdminRegisterDto
        {
            Email = ApiTestSupport.TestAdminEmail,
            Password = ApiTestSupport.TestAdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AdminAuthResponseDto>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.Email.Should().Be(ApiTestSupport.TestAdminEmail);
    }

    [Fact]
    public async Task Register_WhenAdminAlreadyExists_ReturnsConflict()
    {
        await ApiTestSupport.RegisterAdminAndGetTokenAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/Admin/auth/register", new AdminRegisterDto
        {
            Email = "other@inshop.test",
            Password = ApiTestSupport.TestAdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await ApiTestSupport.RegisterAdminAndGetTokenAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/Admin/auth/login", new AdminLoginDto
        {
            Email = ApiTestSupport.TestAdminEmail,
            Password = "WrongPass1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await ApiTestSupport.RegisterAdminAndGetTokenAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/Admin/auth/login", new AdminLoginDto
        {
            Email = ApiTestSupport.TestAdminEmail,
            Password = ApiTestSupport.TestAdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AdminAuthResponseDto>();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
    }
}
