using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;
        private readonly IProductReviewRepository _reviewRepository;
        private readonly IReviewVoteRepository _voteRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IReviewCacheService _reviewCacheService;
        private readonly IMapper _mapper;

        public ReviewService(
            AppDbContext context,
            IProductReviewRepository reviewRepository,
            IReviewVoteRepository voteRepository,
            IProductRepository productRepository,
            IOrderItemRepository orderItemRepository,
            IReviewCacheService reviewCacheService,
            IMapper mapper)
        {
            _context = context;
            _reviewRepository = reviewRepository;
            _voteRepository = voteRepository;
            _productRepository = productRepository;
            _orderItemRepository = orderItemRepository;
            _mapper = mapper;
            _reviewCacheService = reviewCacheService;
        }

        public async Task<(List<ReviewResponseDto> Reviews, int TotalCount)> GetProductReviewsAsync(int productId, int page, int pageSize, int? currentSessionId)
        {
            var reviews = await _reviewRepository.GetByProductIdAsync(productId, (page - 1) * pageSize, pageSize);
            var totalCount = await _reviewRepository.GetCountByProductIdAsync(productId);

            var dtos = new List<ReviewResponseDto>();
            foreach (var r in reviews)
            {
                var voteScore = r.ReviewVotes.Sum(v => v.VoteType);
                var userVote = r.ReviewVotes.FirstOrDefault(v => v.SessionId == currentSessionId)?.VoteType;

                var isVerified = await CheckVerifiedPurchaseAsync(r.SessionId, productId);

                dtos.Add(new ReviewResponseDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    SessionId = r.SessionId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    VoteScore = voteScore,
                    UserVote = userVote,
                    IsVerifiedPurchase = isVerified,
                    // ВАЖНО: Сравниваем ID автора отзыва с ID текущей сессии
                    IsOwner = currentSessionId.HasValue && r.SessionId == currentSessionId.Value
                });
            }

            return (dtos, totalCount);
        }

        public async Task<int> GetReviewCountAsync(int productId)
        {
            return await _reviewRepository.GetCountByProductIdAsync(productId);
        }


        public async Task<ReviewResponseDto> AddReviewAsync(int productId, int sessionId, CreateReviewDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _reviewRepository.ExistsAsync(productId, sessionId))
                {
                    throw new InvalidOperationException("Вы уже оставили отзыв на этот товар.");
                }

                // Маппинг из DTO в сущность
                var review = _mapper.Map<ProductReview>(dto);
                review.ProductId = productId;
                review.SessionId = sessionId;

                await _reviewRepository.AddAsync(review);
                await _reviewRepository.SaveChangesAsync();

                await UpdateProductStatsAsync(productId);
                await transaction.CommitAsync();

                await _reviewCacheService.InvalidateSummaryAsync(productId);

                return await MapToDtoWithExtrasAsync(review, sessionId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ReviewResponseDto> UpdateReviewAsync(int reviewId, int sessionId, UpdateReviewDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null || review.SessionId != sessionId)
                {
                    throw new KeyNotFoundException("Отзыв не найден или нет прав на редактирование.");
                }

                bool ratingChanged = review.Rating != dto.Rating;

                // Маппим обновленные поля из DTO в сущность
                _mapper.Map(dto, review);
                review.UpdatedAt = DateTime.UtcNow;

                _reviewRepository.Update(review);
                await _reviewRepository.SaveChangesAsync();

                if (ratingChanged)
                {
                    await UpdateProductStatsAsync(review.ProductId);
                }

                await transaction.CommitAsync();
                await _reviewCacheService.InvalidateSummaryAsync(review.ProductId);
                return await MapToDtoWithExtrasAsync(review, sessionId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteReviewAsync(int reviewId, int sessionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null || review.SessionId != sessionId)
                {
                    throw new KeyNotFoundException("Отзыв не найден или нет прав на удаление.");
                }

                int productId = review.ProductId;
                _reviewRepository.Remove(review);
                await _reviewRepository.SaveChangesAsync();

                await UpdateProductStatsAsync(productId);
                await transaction.CommitAsync();
                await _reviewCacheService.InvalidateSummaryAsync(productId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || message.Contains("2601", StringComparison.OrdinalIgnoreCase)
                || message.Contains("2627", StringComparison.OrdinalIgnoreCase);
        }

        public async Task VoteReviewAsync(int reviewId, int sessionId, int voteType)
        {
            if (voteType != 1 && voteType != -1)
                throw new ArgumentException("Неверный тип голоса.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingVote = await _voteRepository.GetByReviewAndSessionAsync(reviewId, sessionId);

                if (existingVote != null)
                {
                    if (existingVote.VoteType == voteType)
                    {
                        _voteRepository.Remove(existingVote);
                    }
                    else
                    {
                        existingVote.VoteType = voteType;
                        _voteRepository.Update(existingVote);
                    }
                }
                else
                {
                    var newVote = new ReviewVote
                    {
                        ReviewId = reviewId,
                        SessionId = sessionId,
                        VoteType = voteType
                    };
                    await _voteRepository.AddAsync(newVote);
                }

                await _voteRepository.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<string>> GetRecentReviewTextsAsync(int productId, int count = 50)
        {
            // Используем существующий репозиторий
            var reviews = await _reviewRepository.GetByProductIdAsync(productId, skip: 0, take: count);

            // Извлекаем только текст комментария
            return reviews.Select(r => r.Comment).ToList();
        }

        // --- Приватные методы ---

        private async Task UpdateProductStatsAsync(int productId)
        {
            var stats = await _productRepository.GetReviewStatsAsync(productId);

            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.AverageRating = stats.AverageRating;
                product.ReviewsCount = stats.Count;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<bool> CheckVerifiedPurchaseAsync(int sessionId, int productId)
        {
            return await _orderItemRepository.CheckVerifiedPurchaseAsync(sessionId, productId);
        }

        // Вспомогательный метод для финального маппинга с дополнительными полями
        private async Task<ReviewResponseDto> MapToDtoWithExtrasAsync(ProductReview review, int currentSessionId)
        {
            // Убедимся, что голоса загружены
            await _context.Entry(review).Collection(r => r.ReviewVotes).LoadAsync();

            var dto = _mapper.Map<ReviewResponseDto>(review);

            dto.UserVote = review.ReviewVotes.FirstOrDefault(v => v.SessionId == currentSessionId)?.VoteType;
            dto.IsVerifiedPurchase = await CheckVerifiedPurchaseAsync(review.SessionId, review.ProductId);

            return dto;
        }

    }
}
