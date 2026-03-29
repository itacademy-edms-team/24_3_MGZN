using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

public partial class Product
{
    [Key]
    public int ProductId { get; set; }

    [StringLength(50)]
    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    [Column(TypeName = "money")]
    public decimal ProductPrice { get; set; }

    public bool ProductAvailability { get; set; }

    public int ProductCategoryId { get; set; }

    public int ProductStockQuantity { get; set; }

    [Column("ImageURL")]
    public string? ImageUrl { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("ProductCategoryId")]
    [InverseProperty("Products")]
    public virtual Category ProductCategory { get; set; } = null!;

    [InverseProperty("Product")]
    public virtual ICollection<ProductSpecLink> ProductSpecLinks { get; set; } = new List<ProductSpecLink>();
}
