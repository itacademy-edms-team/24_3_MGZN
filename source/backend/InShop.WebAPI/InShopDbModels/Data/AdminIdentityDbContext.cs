using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Data;

/// <summary>
/// Отдельный контекст только для ASP.NET Identity (админ JWT).
/// НЕ участвует в reverse scaffold — таблицы AspNet* создаются SQL-скриптом в БД.
/// Бизнес-контекст <see cref="AppDbContext"/> можно перегенерировать scaffold без потери Identity.
/// </summary>
public class AdminIdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AdminIdentityDbContext(DbContextOptions<AdminIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Имена таблиц по умолчанию: AspNetUsers, AspNetRoles, AspNetUserRoles, ...
        // Должны совпадать со скриптом scripts/CreateAspNetIdentityTables.sql
    }
}
