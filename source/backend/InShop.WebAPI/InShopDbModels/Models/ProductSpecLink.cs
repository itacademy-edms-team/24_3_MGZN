using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class ProductSpecLink
{
    public int ProductId { get; set; }

    public int SpecId { get; set; }

    public int ValueId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ProductSpecification Spec { get; set; } = null!;

    public virtual ProductSpecValue Value { get; set; } = null!;
}
