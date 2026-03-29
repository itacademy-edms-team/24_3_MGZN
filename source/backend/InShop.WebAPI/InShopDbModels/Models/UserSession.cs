using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

public partial class UserSession
{
    [Key]
    public int SessionId { get; set; }

    [Column("UserIPAddress")]
    [StringLength(45)]
    [Unicode(false)]
    public string UserIpaddress { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    public Guid SessionToken { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    [InverseProperty("Session")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
