using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class ProductSpecification
{
    public int SpecId { get; set; }

    public int GroupId { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public bool? IsFilterable { get; set; }

    public virtual ProductSpecGroup Group { get; set; } = null!;

    public virtual ICollection<ProductSpecLink> ProductSpecLinks { get; set; } = new List<ProductSpecLink>();

    public virtual ICollection<ProductSpecValue> ProductSpecValues { get; set; } = new List<ProductSpecValue>();
}
