using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class UserSession
{
    public int SessionId { get; set; }

    public string UserIpaddress { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public Guid SessionToken { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    public string? UserAgent { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();
}
