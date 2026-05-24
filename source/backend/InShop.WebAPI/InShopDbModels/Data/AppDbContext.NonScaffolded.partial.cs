using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Data;

/// <summary>
/// Всё, что НЕ перегенерируется reverse scaffold (Identity — в AdminIdentityDbContext).
/// Единственная реализация OnModelCreatingPartial для AppDbContext.
/// </summary>
public partial class AppDbContext
{
    public virtual DbSet<OrderAuditLog> OrderAuditLogs { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // ЮKassa
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.YooKassaPaymentId).HasMaxLength(64);
        });

        // Аудит заказов (таблица из scripts/AddAdminIdentityAndAudit.sql)
        modelBuilder.Entity<OrderAuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);

            entity.Property(e => e.OldStatus).HasMaxLength(50);
            entity.Property(e => e.NewStatus).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ChangedBy).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");

            entity.HasIndex(e => e.OrderId, "IX_OrderAuditLogs_OrderId");

            entity.HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrderAuditLogs_Orders");
        });

        // Резервирование остатков (scripts/AddProductReservationColumns.sql)
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.ReservedQuantity).HasDefaultValue(0);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.ImageUrl).HasColumnName("ImageURL").HasMaxLength(500);
        });
    }
}
