using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class ProductSpecValue
{
    public int ValueId { get; set; }

    public int SpecId { get; set; }

    public string? TextValue { get; set; }

    public decimal? NumberValue { get; set; }

    public virtual ICollection<ProductSpecLink> ProductSpecLinks { get; set; } = new List<ProductSpecLink>();

    public virtual ProductSpecification Spec { get; set; } = null!;
}
