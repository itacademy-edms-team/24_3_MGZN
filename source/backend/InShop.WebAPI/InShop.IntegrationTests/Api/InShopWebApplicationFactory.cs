using InShop.WebAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InShop.IntegrationTests.Api;

public sealed class InShopWebApplicationFactory : WebApplicationFactory<Program>
{
    internal const string TestJwtKey = "integration-test-jwt-signing-key-32chars!";

    private readonly string _connectionString;

    public InShopWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting надёжнее AddInMemoryCollection в Test Explorer VS.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:6399,abortConnect=false,connectTimeout=500");
        builder.UseSetting("AiSettings:ApiKey", string.Empty);
        builder.UseSetting("AiSettings:ProviderType", "Yandex");
        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Jwt:Issuer", "InShop.IntegrationTests");
        builder.UseSetting("Jwt:Audience", "InShop.IntegrationTests");
        builder.UseSetting("Jwt:ExpirationHours", "8");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["ConnectionStrings:Redis"] = "127.0.0.1:6399,abortConnect=false,connectTimeout=500",
                ["AiSettings:ApiKey"] = string.Empty,
                ["AiSettings:ProviderType"] = "Yandex",
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "InShop.IntegrationTests",
                ["Jwt:Audience"] = "InShop.IntegrationTests",
                ["Jwt:ExpirationHours"] = "8"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }
        });
    }
}
