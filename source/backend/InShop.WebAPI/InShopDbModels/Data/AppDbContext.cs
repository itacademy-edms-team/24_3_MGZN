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

    public virtual DbSet<ProductSpecGroup> ProductSpecGroups { get; set; }

    public virtual DbSet<ProductSpecLink> ProductSpecLinks { get; set; }

    public virtual DbSet<ProductSpecValue> ProductSpecValues { get; set; }

    public virtual DbSet<ProductSpecification> ProductSpecifications { get; set; }

    public virtual DbSet<ShipCompany> ShipCompanies { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-G67DLTD;Database=InShopDB;Integrated Security=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.Property(e => e.HashPassword).IsFixedLength();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderTotalAmount).HasDefaultValueSql("((0.0))");
            entity.Property(e => e.PayStatus).HasDefaultValue("Не оплачен");

            entity.HasOne(d => d.Session).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_UserSession");

            entity.HasOne(d => d.ShipCompany).WithMany(p => p.Orders).HasConstraintName("FK_Orders_Ship_Companies");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("Order_Items", tb =>
                {
                    tb.HasTrigger("SetInitialProductPrice");
                    tb.HasTrigger("trg_UpdateOrderTotal");
                });

            entity.Property(e => e.TotalPrice).HasComputedColumnSql("([QuantityItem]*[Price])", true);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Items_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasOne(d => d.ProductCategory).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories1");
        });

        modelBuilder.Entity<ProductSpecGroup>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("PK__ProductS__149AF36A6FC5BCBD");

            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<ProductSpecLink>(entity =>
        {
            entity.HasOne(d => d.Product).WithMany(p => p.ProductSpecLinks).HasConstraintName("FK_ProductSpecLinks_Products");

            entity.HasOne(d => d.Spec).WithMany(p => p.ProductSpecLinks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSpecLinks_Specifications");

            entity.HasOne(d => d.Value).WithMany(p => p.ProductSpecLinks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSpecLinks_Values");
        });

        modelBuilder.Entity<ProductSpecValue>(entity =>
        {
            entity.HasKey(e => e.ValueId).HasName("PK__ProductS__93364E482325D161");

            entity.HasOne(d => d.Spec).WithMany(p => p.ProductSpecValues).HasConstraintName("FK_ProductSpecValues_Specifications");
        });

        modelBuilder.Entity<ProductSpecification>(entity =>
        {
            entity.HasKey(e => e.SpecId).HasName("PK__ProductS__883D567B66614627");

            entity.Property(e => e.IsFilterable).HasDefaultValue(true);

            entity.HasOne(d => d.Group).WithMany(p => p.ProductSpecifications).HasConstraintName("FK_ProductSpecifications_Groups");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK_UserSession");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SessionToken).HasDefaultValueSql("(newid())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
