using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InShopDbModels.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    public decimal ProductPrice { get; set; }

    public bool ProductAvailability { get; set; }

    public int ProductCategoryId { get; set; }

    /// <summary>
    /// Свободный (не зарезервированный) остаток, доступный для нового резервирования.
    /// В терминах инвентаризации соответствует «Quantity» в схеме резервирования.
    /// </summary>
    public int ProductStockQuantity { get; set; }

    /// <summary>
    /// Количество единиц, зарезервированных под заказы, но ещё не списанных окончательно (Finalize).
    /// Физический остаток на складе = ProductStockQuantity + ReservedQuantity.
    /// </summary>
    public int ReservedQuantity { get; set; }

    /// <summary>
    /// Токен оптимистичной блокировки (SQL Server rowversion).
    /// EF Core сравнивает RowVersion при UPDATE; при параллельном изменении строки — DbUpdateConcurrencyException.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public decimal AverageRating { get; set; }

    public int ReviewsCount { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Category ProductCategory { get; set; } = null!;

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<ProductSpecLink> ProductSpecLinks { get; set; } = new List<ProductSpecLink>();
}
