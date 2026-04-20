using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IReviewVoteRepository
    {
        Task<ReviewVote?> GetByReviewAndSessionAsync(int reviewId, int sessionId);
        Task AddAsync(ReviewVote vote);
        void Update(ReviewVote vote);
        void Remove(ReviewVote vote);
        Task SaveChangesAsync();
    }
}
