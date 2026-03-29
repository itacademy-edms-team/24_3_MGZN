using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

[PrimaryKey("ProductId", "SpecId")]
[Index("ProductId", Name = "IX_ProductSpecLinks_ProductId")]
public partial class ProductSpecLink
{
    [Key]
    public int ProductId { get; set; }

    [Key]
    public int SpecId { get; set; }

    public int ValueId { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductSpecLinks")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("SpecId")]
    [InverseProperty("ProductSpecLinks")]
    public virtual ProductSpecification Spec { get; set; } = null!;

    [ForeignKey("ValueId")]
    [InverseProperty("ProductSpecLinks")]
    public virtual ProductSpecValue Value { get; set; } = null!;
}
