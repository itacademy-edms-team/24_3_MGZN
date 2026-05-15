using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Data;

/// <summary>
/// Конфигурация EF для колонки ЮKassa (DB-first: колонка сначала в БД, затем маппинг здесь).
/// </summary>
public partial class AppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.YooKassaPaymentId).HasMaxLength(64);
        });
    }
}
