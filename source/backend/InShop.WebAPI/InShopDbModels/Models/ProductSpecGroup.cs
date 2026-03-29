using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

[Index("CategoryName", Name = "UK_ProductSpecGroups_CategoryName", IsUnique = true)]
[Index("CategoryName", Name = "UQ__ProductS__8517B2E0A84B6E0F", IsUnique = true)]
public partial class ProductSpecGroup
{
    [Key]
    public int GroupId { get; set; }

    [StringLength(50)]
    public string CategoryName { get; set; } = null!;

    public int? SortOrder { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();
}
