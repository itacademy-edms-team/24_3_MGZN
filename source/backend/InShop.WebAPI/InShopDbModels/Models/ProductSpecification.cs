using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

[Index("GroupId", "Name", Name = "UK_ProductSpecifications_NameInGroup", IsUnique = true)]
public partial class ProductSpecification
{
    [Key]
    public int SpecId { get; set; }

    public int GroupId { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [StringLength(20)]
    public string DataType { get; set; } = null!;

    public bool? IsFilterable { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("ProductSpecifications")]
    public virtual ProductSpecGroup Group { get; set; } = null!;

    [InverseProperty("Spec")]
    public virtual ICollection<ProductSpecLink> ProductSpecLinks { get; set; } = new List<ProductSpecLink>();

    [InverseProperty("Spec")]
    public virtual ICollection<ProductSpecValue> ProductSpecValues { get; set; } = new List<ProductSpecValue>();
}
