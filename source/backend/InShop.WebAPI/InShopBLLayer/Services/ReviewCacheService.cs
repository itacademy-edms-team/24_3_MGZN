using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class ReviewCacheService : IReviewCacheService
    {
        private sealed class CachedReviewSummary
        {
            public ReviewSummaryDto Summary { get; set; } = new();
            public int ReviewCount { get; set; }
        }

        private readonly IDatabase _redisDb;
        private readonly ILogger<ReviewCacheService> _logger;

        private const string SUMMARY_PREFIX = "review:summary:";
        private const string LOCK_PREFIX = "lock:review:";

        public ReviewCacheService(IConnectionMultiplexer redis, ILogger<ReviewCacheService> logger)
        {
            _redisDb = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<(ReviewSummaryDto? Summary, int? ReviewCount)> GetSummaryAsync(int productId)
        {
            try
            {
                var key = $"{SUMMARY_PREFIX}{productId}";
                var json = await _redisDb.StringGetAsync(key);

                if (json.IsNullOrEmpty) return (null, null);

                // Используем опции, игнорирующие регистр свойств JSON
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var cached = JsonSerializer.Deserialize<CachedReviewSummary>(json!, options);
                return (cached?.Summary, cached?.ReviewCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review summary from Redis for ProductId {ProductId}", productId);
                return (null, null);
            }
        }

        public async Task SetSummaryAsync(int productId, ReviewSummaryDto summary, int reviewCount, TimeSpan ttl)
        {
            try
            {
                var key = $"{SUMMARY_PREFIX}{productId}";
                var payload = new CachedReviewSummary
                {
                    Summary = summary,
                    ReviewCount = reviewCount
                };
                var json = JsonSerializer.Serialize(payload);
                await _redisDb.StringSetAsync(key, json, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting review summary in Redis for ProductId {ProductId}", productId);
            }
        }

        public async Task InvalidateSummaryAsync(int productId)
        {
            try
            {
                var key = $"{SUMMARY_PREFIX}{productId}";
                await _redisDb.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating review summary in Redis for ProductId {ProductId}", productId);
            }
        }

        public async Task<bool> TryAcquireLockAsync(int productId, TimeSpan timeout)
        {
            var key = $"{LOCK_PREFIX}{productId}";
            // LockTake возвращает true, если блокировка успешно установлена
            return await _redisDb.LockTakeAsync(key, Environment.MachineName, timeout);
        }

        public async Task ReleaseLockAsync(int productId)
        {
            var key = $"{LOCK_PREFIX}{productId}";
            await _redisDb.LockReleaseAsync(key, Environment.MachineName);
        }
    }
}
