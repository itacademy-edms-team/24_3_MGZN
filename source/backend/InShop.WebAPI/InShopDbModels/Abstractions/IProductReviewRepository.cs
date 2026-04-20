using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IProductReviewRepository
    {
        Task<ProductReview> GetByIdAsync(int id);
        Task<List<ProductReview>> GetByProductIdAsync(int productId, int skip, int take);
        Task<int> GetCountByProductIdAsync(int productId);
        Task AddAsync(ProductReview review);
        void Update(ProductReview review);
        void Remove(ProductReview review);
        Task<bool> ExistsAsync(int productId, int sessionId);
        Task SaveChangesAsync();
    }
}
