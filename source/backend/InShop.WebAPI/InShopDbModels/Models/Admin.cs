using System;
using System.Collections.Generic;

namespace InShopDataLayer.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public string AdminUsername { get; set; } = null!;

    public byte[] HashPassword { get; set; } = null!;
}
