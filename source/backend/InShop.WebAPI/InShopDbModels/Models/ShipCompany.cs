using System;
using System.Collections.Generic;

namespace InShopDataLayer.Models;

public partial class ShipCompany
{
    public int ShipCompanyId { get; set; }

    public string ShipCompanyName { get; set; } = null!;

    public string Contact { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
