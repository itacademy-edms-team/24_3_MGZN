using InShopDbModels.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Testcontainers.MsSql;

namespace InShop.IntegrationTests.Infrastructure;

/// <summary>
/// Общая фикстура: один контейнер SQL Server на всю коллекцию integration-тестов.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "InShopIntegrationTests";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    private readonly SemaphoreSlim _databaseGate = new(1, 1);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var baseBuilder = new SqlConnectionStringBuilder(_container.GetConnectionString());
        ConnectionString = new SqlConnectionStringBuilder(baseBuilder.ConnectionString)
        {
            InitialCatalog = TestDatabaseName,
            TrustServerCertificate = true
        }.ConnectionString;

        await EnsureTestDatabaseExistsAsync(baseBuilder);

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        // AppDbContext создаёт БД первым — второй EnsureCreated для Identity ничего не делает.
        await using var identityContext = CreateIdentityContext();
        await EnsureIdentitySchemaAsync(identityContext);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public AdminIdentityDbContext CreateIdentityContext()
    {
        var options = new DbContextOptionsBuilder<AdminIdentityDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AdminIdentityDbContext(options);
    }

    /// <summary>
    /// Очищает данные между тестами без DROP DATABASE (иначе падает на master в Testcontainers).
    /// Сериализовано через lock: при пакетном прогоне параллельные Reset давали 404 в API-тестах.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await _databaseGate.WaitAsync();
        try
        {
            await using var context = CreateContext();

            await context.ReviewVotes.ExecuteDeleteAsync();
            await context.OrderAuditLogs.ExecuteDeleteAsync();
            await context.OrderItems.ExecuteDeleteAsync();
            await context.ProductReviews.ExecuteDeleteAsync();
            await context.Orders.ExecuteDeleteAsync();
            await context.ProductSpecLinks.ExecuteDeleteAsync();
            await context.Products.ExecuteDeleteAsync();
            await context.Categories.ExecuteDeleteAsync();
            await context.ShipCompanies.ExecuteDeleteAsync();
            await context.UserSessions.ExecuteDeleteAsync();
        }
        finally
        {
            _databaseGate.Release();
        }
    }

    /// <summary>Очищает пользователей Identity между admin API-тестами (роли AspNetRoles сохраняются).</summary>
    public async Task ClearIdentityUsersAsync()
    {
        await using var context = CreateIdentityContext();
        await context.Database.ExecuteSqlRawAsync("""
            DELETE FROM AspNetUserRoles;
            DELETE FROM AspNetUserClaims;
            DELETE FROM AspNetUserLogins;
            DELETE FROM AspNetUserTokens;
            DELETE FROM AspNetUsers;
            """);
    }

    /// <summary>
    /// Атомарно: reset + действие (seed) под тем же lock, чтобы HTTP-тест не попал в пустую БД.
    /// </summary>
    public async Task<T> WithFreshDatabaseAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await _databaseGate.WaitAsync();
        try
        {
            await using var context = CreateContext();

            await context.ReviewVotes.ExecuteDeleteAsync();
            await context.OrderAuditLogs.ExecuteDeleteAsync();
            await context.OrderItems.ExecuteDeleteAsync();
            await context.ProductReviews.ExecuteDeleteAsync();
            await context.Orders.ExecuteDeleteAsync();
            await context.ProductSpecLinks.ExecuteDeleteAsync();
            await context.Products.ExecuteDeleteAsync();
            await context.Categories.ExecuteDeleteAsync();
            await context.ShipCompanies.ExecuteDeleteAsync();
            await context.UserSessions.ExecuteDeleteAsync();

            return await action(context);
        }
        finally
        {
            _databaseGate.Release();
        }
    }

    private static async Task EnsureIdentitySchemaAsync(AdminIdentityDbContext identityContext)
    {
        if (await IdentityTablesExistAsync(identityContext))
        {
            return;
        }

        var creator = identityContext.Database.GetService<IRelationalDatabaseCreator>();
        await creator.CreateTablesAsync();
    }

    private static async Task<bool> IdentityTablesExistAsync(AdminIdentityDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NOT NULL THEN 1 ELSE 0 END
            """;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) == 1;
    }

    private static async Task EnsureTestDatabaseExistsAsync(SqlConnectionStringBuilder baseBuilder)
    {
        var masterBuilder = new SqlConnectionStringBuilder(baseBuilder.ConnectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{TestDatabaseName}') IS NULL
                CREATE DATABASE [{TestDatabaseName}];
            """;
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("SqlServer")]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
