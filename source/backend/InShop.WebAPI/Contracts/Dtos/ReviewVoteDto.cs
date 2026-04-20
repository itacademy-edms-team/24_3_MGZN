using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class ReviewVoteDto
    {
        // 1 = Upvote (Полезно), -1 = Downvote (Не полезно)
        public int VoteType { get; set; }
    }
}
