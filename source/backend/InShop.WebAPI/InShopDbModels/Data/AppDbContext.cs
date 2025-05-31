using System;
using System.Collections.Generic;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ShipCompany> ShipCompanies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-AR0BS4O;Database=InShopDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.Property(e => e.AdminUsername)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HashPassword)
                .HasMaxLength(32)
                .IsFixedLength();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.ImageURL).HasMaxLength(50);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.CustomerEmail).HasMaxLength(250);
            entity.Property(e => e.CustomerFullname).HasMaxLength(50);
            entity.Property(e => e.CustomerPhoneNumber).HasMaxLength(50);
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50).HasConversion<string>();
            entity.Property(e => e.OrderTotalAmount)
                .HasDefaultValueSql("((0.0))")
                .HasColumnType("money");
            entity.Property(e => e.PayMethod).HasMaxLength(50);
            entity.Property(e => e.PayStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Не оплачен");
            entity.Property(e => e.ShipAddress).HasMaxLength(500);
            entity.Property(e => e.ShipMethod).HasMaxLength(50);

            entity.HasOne(d => d.ShipCompany).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShipCompanyId)
                .HasConstraintName("FK_Orders_Ship_Companies");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("Order_Items", tb =>
                {
                    tb.HasTrigger("SetInitialProductPrice");
                    tb.HasTrigger("trg_UpdateOrderTotal");
                });

            entity.Property(e => e.Price).HasColumnType("money");
            entity.Property(e => e.TotalPrice)
                .HasComputedColumnSql("([QuantityItem]*[Price])", true)
                .HasColumnType("money");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Items_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Items_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.ImageUrl).HasColumnName("ImageURL");
            entity.Property(e => e.ProductName).HasMaxLength(50);
            entity.Property(e => e.ProductPrice).HasColumnType("money");

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories1");
        });

        modelBuilder.Entity<ShipCompany>(entity =>
        {
            entity.ToTable("Ship_Companies");

            entity.Property(e => e.Contact).HasMaxLength(500);
            entity.Property(e => e.ShipCompanyName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
