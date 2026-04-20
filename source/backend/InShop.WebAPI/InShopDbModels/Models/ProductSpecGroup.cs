using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class ProductSpecGroup
{
    public int GroupId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int? SortOrder { get; set; }

    public virtual ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();
}
