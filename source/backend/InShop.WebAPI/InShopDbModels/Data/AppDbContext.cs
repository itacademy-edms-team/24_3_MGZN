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

    public virtual DbSet<ProductReview> ProductReviews { get; set; }

    public virtual DbSet<ProductSpecGroup> ProductSpecGroups { get; set; }

    public virtual DbSet<ProductSpecLink> ProductSpecLinks { get; set; }

    public virtual DbSet<ProductSpecValue> ProductSpecValues { get; set; }

    public virtual DbSet<ProductSpecification> ProductSpecifications { get; set; }

    public virtual DbSet<ReviewVote> ReviewVotes { get; set; }

    public virtual DbSet<ShipCompany> ShipCompanies { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-G67DLTD;Database=InShopDB;Integrated Security=True;TrustServerCertificate=True");

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
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(50)
                .HasColumnName("ImageURL");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.CustomerEmail).HasMaxLength(250);
            entity.Property(e => e.CustomerFullname).HasMaxLength(50);
            entity.Property(e => e.CustomerPhoneNumber).HasMaxLength(50);
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderStatus).HasMaxLength(50);
            entity.Property(e => e.OrderTotalAmount)
                .HasDefaultValueSql("((0.0))")
                .HasColumnType("money");
            entity.Property(e => e.PayMethod).HasMaxLength(50);
            entity.Property(e => e.PayStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Не оплачен");
            entity.Property(e => e.ShipAddress).HasMaxLength(500);
            entity.Property(e => e.ShipMethod).HasMaxLength(50);

            entity.HasOne(d => d.Session).WithMany(p => p.Orders)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_UserSession");

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
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Items_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.ImageUrl).HasColumnName("ImageURL");
            entity.Property(e => e.ProductName).HasMaxLength(50);
            entity.Property(e => e.ProductPrice).HasColumnType("money");

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories1");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__ProductR__74BC79CEB0B86AEA");

            entity.HasIndex(e => e.ProductId, "IX_ProductReviews_ProductId");

            entity.HasIndex(e => e.SessionId, "IX_ProductReviews_SessionId");

            entity.HasIndex(e => new { e.ProductId, e.SessionId }, "UQ_ProductReview_Product_Session").IsUnique();

            entity.Property(e => e.Comment).HasMaxLength(4000);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductReviews_Products");

            entity.HasOne(d => d.Session).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_ProductReviews_Sessions");
        });

        modelBuilder.Entity<ProductSpecGroup>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("PK__ProductS__149AF36A6FC5BCBD");

            entity.HasIndex(e => e.CategoryName, "UK_ProductSpecGroups_CategoryName").IsUnique();

            entity.HasIndex(e => e.CategoryName, "UQ__ProductS__8517B2E0A84B6E0F").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<ProductSpecLink>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.SpecId });

            entity.HasIndex(e => e.ProductId, "IX_ProductSpecLinks_ProductId");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSpecLinks)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductSpecLinks_Products");

            entity.HasOne(d => d.Spec).WithMany(p => p.ProductSpecLinks)
                .HasForeignKey(d => d.SpecId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSpecLinks_Specifications");

            entity.HasOne(d => d.Value).WithMany(p => p.ProductSpecLinks)
                .HasForeignKey(d => d.ValueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSpecLinks_Values");
        });

        modelBuilder.Entity<ProductSpecValue>(entity =>
        {
            entity.HasKey(e => e.ValueId).HasName("PK__ProductS__93364E482325D161");

            entity.HasIndex(e => e.NumberValue, "IX_ProductSpecValues_NumberValue");

            entity.HasIndex(e => e.TextValue, "IX_ProductSpecValues_TextValue");

            entity.Property(e => e.NumberValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TextValue).HasMaxLength(255);

            entity.HasOne(d => d.Spec).WithMany(p => p.ProductSpecValues)
                .HasForeignKey(d => d.SpecId)
                .HasConstraintName("FK_ProductSpecValues_Specifications");
        });

        modelBuilder.Entity<ProductSpecification>(entity =>
        {
            entity.HasKey(e => e.SpecId).HasName("PK__ProductS__883D567B66614627");

            entity.HasIndex(e => new { e.GroupId, e.Name }, "UK_ProductSpecifications_NameInGroup").IsUnique();

            entity.Property(e => e.DataType).HasMaxLength(20);
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.IsFilterable).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Group).WithMany(p => p.ProductSpecifications)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_ProductSpecifications_Groups");
        });

        modelBuilder.Entity<ReviewVote>(entity =>
        {
            entity.HasKey(e => e.VoteId).HasName("PK__ReviewVo__52F015C2D10101AD");

            entity.HasIndex(e => e.ReviewId, "IX_ReviewVotes_ReviewId");

            entity.HasIndex(e => new { e.ReviewId, e.SessionId }, "UQ_ReviewVote_Review_Session").IsUnique();

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewVotes)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("FK_ReviewVotes_Reviews");

            entity.HasOne(d => d.Session).WithMany(p => p.ReviewVotes)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewVotes_Sessions");
        });

        modelBuilder.Entity<ShipCompany>(entity =>
        {
            entity.ToTable("Ship_Companies");

            entity.Property(e => e.Contact).HasMaxLength(500);
            entity.Property(e => e.ShipCompanyName).HasMaxLength(100);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK_UserSession");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SessionToken).HasDefaultValueSql("(newid())");
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserIpaddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("UserIPAddress");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
