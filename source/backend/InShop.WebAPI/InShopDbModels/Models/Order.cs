﻿using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;
public partial class Order
{
    public int OrderId { get; set; }

    public OrderStatusType OrderStatus { get; set; }

    public DateOnly OrderDate { get; set; }

    public int? ShipCompanyId { get; set; }

    public string? ShipAddress { get; set; }

    public DateOnly? ShipDate { get; set; }

    public string ShipMethod { get; set; } = null!;

    public string PayStatus { get; set; } = null!;

    public string CustomerFullname { get; set; } = null!;

    public string PayMethod { get; set; } = null!;

    public string CustomerEmail { get; set; } = null!;

    public string CustomerPhoneNumber { get; set; } = null!;

    public decimal OrderTotalAmount { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ShipCompany? ShipCompany { get; set; }
}
