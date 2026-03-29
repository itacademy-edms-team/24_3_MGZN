using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

[Table("Ship_Companies")]
public partial class ShipCompany
{
    [Key]
    public int ShipCompanyId { get; set; }

    [StringLength(100)]
    public string ShipCompanyName { get; set; } = null!;

    [StringLength(500)]
    public string Contact { get; set; } = null!;

    [InverseProperty("ShipCompany")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
