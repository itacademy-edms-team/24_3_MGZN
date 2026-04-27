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
        private readonly IDatabase _redisDb;
        private readonly ILogger<ReviewCacheService> _logger;

        private const string SUMMARY_PREFIX = "review:summary:";
        private const string LOCK_PREFIX = "lock:review:";

        public ReviewCacheService(IConnectionMultiplexer redis, ILogger<ReviewCacheService> logger)
        {
            _redisDb = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<ReviewSummaryDto?> GetSummaryAsync(int productId)
        {
            try
            {
                var key = $"{SUMMARY_PREFIX}{productId}";
                var json = await _redisDb.StringGetAsync(key);

                if (json.IsNullOrEmpty) return null;

                // Используем опции, игнорирующие регистр свойств JSON
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<ReviewSummaryDto>(json!, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review summary from Redis for ProductId {ProductId}", productId);
                return null;
            }
        }

        public async Task SetSummaryAsync(int productId, ReviewSummaryDto summary, TimeSpan ttl)
        {
            try
            {
                var key = $"{SUMMARY_PREFIX}{productId}";
                var json = JsonSerializer.Serialize(summary);
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
