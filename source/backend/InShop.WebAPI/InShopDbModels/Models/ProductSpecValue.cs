using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

[Index("NumberValue", Name = "IX_ProductSpecValues_NumberValue")]
[Index("TextValue", Name = "IX_ProductSpecValues_TextValue")]
public partial class ProductSpecValue
{
    [Key]
    public int ValueId { get; set; }

    public int SpecId { get; set; }

    [StringLength(255)]
    public string? TextValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? NumberValue { get; set; }

    [InverseProperty("Value")]
    public virtual ICollection<ProductSpecLink> ProductSpecLinks { get; set; } = new List<ProductSpecLink>();

    [ForeignKey("SpecId")]
    [InverseProperty("ProductSpecValues")]
    public virtual ProductSpecification Spec { get; set; } = null!;
}
