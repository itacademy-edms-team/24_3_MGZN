using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InShopDbModels.Models;

public partial class Admin
{
    [Key]
    public int AdminId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string AdminUsername { get; set; } = null!;

    [MaxLength(32)]
    public byte[] HashPassword { get; set; } = null!;
}
