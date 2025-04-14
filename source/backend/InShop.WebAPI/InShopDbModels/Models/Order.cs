﻿using System;
using System.Collections.Generic;

namespace InShopDataLayer.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderStatus { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    public int? ShipCompanyId { get; set; }

    public string? ShipAddres { get; set; }

    public DateOnly? ShipDate { get; set; }

    public string ShipMethod { get; set; } = null!;

    public string PayStatus { get; set; } = null!;

    public string CustomerFullname { get; set; } = null!;

    public decimal OrderTotalAmount { get; set; }

    public string PayMethod { get; set; } = null!;

    public string CustomerEmail { get; set; } = null!;

    public string CustomerPhoneNumber { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ShipCompany? ShipCompany { get; set; }
}
