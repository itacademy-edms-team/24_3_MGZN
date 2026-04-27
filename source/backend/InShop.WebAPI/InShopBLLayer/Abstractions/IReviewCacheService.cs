using Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IReviewCacheService
    {
        Task<ReviewSummaryDto?> GetSummaryAsync(int productId);
        Task SetSummaryAsync(int productId, ReviewSummaryDto summary, TimeSpan ttl);
        Task InvalidateSummaryAsync(int productId);
        Task<bool> TryAcquireLockAsync(int productId, TimeSpan timeout);
        Task ReleaseLockAsync(int productId);
    }
}
