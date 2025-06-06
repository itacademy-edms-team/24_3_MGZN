﻿using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int QuantityItem { get; set; }

    public decimal Price { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
