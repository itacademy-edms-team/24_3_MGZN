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
    public class ProductReviewRepository : IProductReviewRepository
    {
        private readonly AppDbContext _context;

        public ProductReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductReview> GetByIdAsync(int id)
        {
            return await _context.ProductReviews
                .Include(r => r.ReviewVotes) // Загружаем голоса сразу, если нужно
                .FirstOrDefaultAsync(r => r.ReviewId == id);
        }

        public async Task<List<ProductReview>> GetByProductIdAsync(int productId, int skip, int take)
        {
            return await _context.ProductReviews
                .Include(r => r.ReviewVotes)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt) // Сортировку по полезности лучше делать в сервисе или через сложный запрос, но пока базовая по дате
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountByProductIdAsync(int productId)
        {
            return await _context.ProductReviews.CountAsync(r => r.ProductId == productId);
        }

        public async Task AddAsync(ProductReview review)
        {
            await _context.ProductReviews.AddAsync(review);
        }

        public void Update(ProductReview review)
        {
            _context.ProductReviews.Update(review);
        }

        public void Remove(ProductReview review)
        {
            _context.ProductReviews.Remove(review);
        }

        public async Task<bool> ExistsAsync(int productId, int sessionId)
        {
            return await _context.ProductReviews.AnyAsync(r => r.ProductId == productId && r.SessionId == sessionId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
