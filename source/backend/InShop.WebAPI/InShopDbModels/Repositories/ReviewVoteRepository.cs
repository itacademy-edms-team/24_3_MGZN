using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Repositories
{
    public class ReviewVoteRepository : IReviewVoteRepository
    {
        private readonly AppDbContext _appDbContext;

        public ReviewVoteRepository(AppDbContext context)
        {
            _appDbContext = context;
        }

        public async Task<ReviewVote?> GetByReviewAndSessionAsync(int reviewId, int sessionId)
        {
            return await _appDbContext.ReviewVotes
                .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.SessionId == sessionId);
        }

        public async Task AddAsync(ReviewVote vote)
        {
            await _appDbContext.ReviewVotes.AddAsync(vote);
        }

        public void Update(ReviewVote vote)
        {
            _appDbContext.ReviewVotes.Update(vote);
        }

        public void Remove(ReviewVote vote)
        {
            _appDbContext.ReviewVotes.Remove(vote);
        }

        public async Task SaveChangesAsync()
        {
            await _appDbContext.SaveChangesAsync();
        }
    }
}
