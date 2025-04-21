using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    public decimal ProductPrice { get; set; }

    public bool ProductAvailability { get; set; }

    public int ProductCategoryId { get; set; }

    public int ProductStockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Category ProductCategory { get; set; } = null!;
}
