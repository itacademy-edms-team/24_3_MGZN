using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class UserSession
{
    public int SessionId { get; set; }

    public string UserIpaddress { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
