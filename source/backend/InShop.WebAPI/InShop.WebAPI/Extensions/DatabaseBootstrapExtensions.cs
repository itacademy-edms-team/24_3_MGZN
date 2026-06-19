using InShopDbModels.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace InShop.WebAPI.Extensions;

public static class DatabaseBootstrapExtensions
{
    public static async Task EnsureDatabaseCreatedForDockerAsync(
        this IServiceProvider services,
        IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Database:EnsureCreated"))
        {
            return;
        }

        using var scope = services.CreateScope();
        var appContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await appContext.Database.EnsureCreatedAsync();

        var identityContext = scope.ServiceProvider.GetRequiredService<AdminIdentityDbContext>();
        if (!await IdentityTablesExistAsync(identityContext))
        {
            var creator = identityContext.Database.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
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
}
