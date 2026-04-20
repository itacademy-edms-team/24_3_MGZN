using System;
using System.Collections.Generic;

namespace InShopDbModels.Models;

public partial class ReviewVote
{
    public int VoteId { get; set; }

    public int ReviewId { get; set; }

    public int SessionId { get; set; }

    public int VoteType { get; set; }

    public virtual ProductReview Review { get; set; } = null!;

    public virtual UserSession Session { get; set; } = null!;
}
