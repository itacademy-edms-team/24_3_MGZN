using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public string AdminUsername { get; set; } = null!;

    public byte[] HashPassword { get; set; } = null!;
}
