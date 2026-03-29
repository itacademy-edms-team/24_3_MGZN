using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

public partial class Order
{
    [Key]
    public int OrderId { get; set; }

    [StringLength(50)]
    public string OrderStatus { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    public int? ShipCompanyId { get; set; }

    [StringLength(500)]
    public string? ShipAddress { get; set; }

    public DateOnly? ShipDate { get; set; }

    [StringLength(50)]
    public string ShipMethod { get; set; } = null!;

    [StringLength(50)]
    public string PayStatus { get; set; } = null!;

    [StringLength(50)]
    public string CustomerFullname { get; set; } = null!;

    [StringLength(50)]
    public string PayMethod { get; set; } = null!;

    [StringLength(250)]
    public string CustomerEmail { get; set; } = null!;

    [StringLength(50)]
    public string CustomerPhoneNumber { get; set; } = null!;

    [Column(TypeName = "money")]
    public decimal OrderTotalAmount { get; set; }

    public int SessionId { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("SessionId")]
    [InverseProperty("Orders")]
    public virtual UserSession Session { get; set; } = null!;

    [ForeignKey("ShipCompanyId")]
    [InverseProperty("Orders")]
    public virtual ShipCompany? ShipCompany { get; set; }
}
