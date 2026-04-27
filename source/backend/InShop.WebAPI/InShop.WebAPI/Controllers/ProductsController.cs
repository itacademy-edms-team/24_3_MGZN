using Contracts.Dtos;
using InShop.WebAPI.Extensions;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Services;
using InShopDbModels.Abstractions;
using InShopDbModels.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        private readonly IReviewCacheService _reviewCacheService;
        private readonly IAiAnalysisService _aiAnalysisService;
        private readonly ILogger<ProductsController> _logger;
        public ProductsController(IProductService productService, 
            IReviewService reviewService,
            IReviewCacheService reviewCacheService,
            IAiAnalysisService aiAnalysisService,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _reviewService = reviewService;
            _reviewCacheService = reviewCacheService;
            _aiAnalysisService = aiAnalysisService;
            _logger = logger;

        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _productService.GetProduct(id);
            return product == null ? NotFound() : Ok(product);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetProducts();
            return Ok(products);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto productDto)
        {
            await _productService.CreateProduct(productDto);
            return Ok("Товар Создан");
        }
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProductDto productDto)
        {
            await _productService.UpdateProduct(productDto);
            return Ok("Информация о товаре обновлена");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProduct(id);
            return Ok("Товар удалён");
        }
        [HttpGet("products-by-category")]
        public async Task<IActionResult> GetProductsByCategory(
            [FromQuery] string categoryName,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? inStock = null, // Добавляем параметр inStock
            [FromQuery] string sortBy = "ProductName",
            [FromQuery] string sortOrder = "asc")
        {
            try
            {
                // Валидация sortBy
                var allowedSortColumns = new[] { "ProductName", "Price" };
                if (!allowedSortColumns.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Invalid sort column. Allowed: ProductName, Price" });
                }

                // Валидация sortOrder
                var allowedSortOrders = new[] { "asc", "desc" };
                if (!allowedSortOrders.Contains(sortOrder, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Invalid sort order. Allowed: asc, desc" });
                }

                // Валидация цен
                if (minPrice.HasValue && minPrice.Value < 0)
                {
                    return BadRequest(new { message = "Min price cannot be negative" });
                }

                if (maxPrice.HasValue && maxPrice.Value < 0)
                {
                    return BadRequest(new { message = "Max price cannot be negative" });
                }

                if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
                {
                    return BadRequest(new { message = "Min price cannot be greater than max price" });
                }

                var productsByCategory = await _productService.GetProductsByCategoryName(
                    categoryName,
                    minPrice,
                    maxPrice,
                    inStock,
                    sortBy,
                    sortOrder);

                return Ok(productsByCategory);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("random-products")]
        public async Task<IActionResult> GetRandom()
        {
            var rndProducts = await _productService.GetRandomProducts();
            return Ok(rndProducts);
        }
        [HttpGet("{id}/specifications")]
        public async Task<IActionResult> GetProductSpecifications(int id)
        {
            var specs = await _productService.GetProductSpecificationsAsync(id);

            if (specs == null)
            {
                return NotFound(new { message = "Товар не найден или не имеет характеристик." });
            }

            return Ok(specs);
        }

        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetProductReviews(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1)
            {
                return BadRequest(new { message = "Параметр page должен быть >= 1." });
            }

            if (pageSize < 1 || pageSize > 50)
            {
                return BadRequest(new { message = "Параметр pageSize должен быть в диапазоне 1..50." });
            }

            // Берем SessionId из контекста (он там благодаря Middleware)
            int? sessionId = HttpContext.GetSessionId();

            var result = await _reviewService.GetProductReviewsAsync(id, page, pageSize, sessionId);

            return Ok(new
            {
                reviews = result.Reviews,
                totalCount = result.TotalCount,
                page,
                pageSize
            });
        }

        [HttpPost("{id}/reviews")]
        public async Task<IActionResult> CreateReview(int id, [FromBody] CreateReviewDto dto)
        {
            int? sessionId = HttpContext.GetSessionId();
            if (!sessionId.HasValue)
            {
                return Unauthorized(new { message = "Требуется активная сессия" });
            }

            try
            {
                var review = await _reviewService.AddReviewAsync(id, sessionId.Value, dto);
                return CreatedAtAction(nameof(GetProductReviews), new { id }, review);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (ReviewService.IsUniqueConstraintViolation(ex))
            {
                return Conflict(new { message = "Вы уже оставили отзыв на этот товар." });
            }
        }

        [HttpPut("reviews/{reviewId}")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewDto dto)
        {
            int? sessionId = HttpContext.GetSessionId();
            if (!sessionId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var review = await _reviewService.UpdateReviewAsync(reviewId, sessionId.Value, dto);
                return Ok(review);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("reviews/{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            int? sessionId = HttpContext.GetSessionId();
            if (!sessionId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                await _reviewService.DeleteReviewAsync(reviewId, sessionId.Value);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("reviews/{reviewId}/vote")]
        public async Task<IActionResult> VoteReview(int reviewId, [FromBody] ReviewVoteDto dto)
        {
            int? sessionId = HttpContext.GetSessionId();
            if (!sessionId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                await _reviewService.VoteReviewAsync(reviewId, sessionId.Value, dto.VoteType);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex) when (ReviewService.IsUniqueConstraintViolation(ex))
            {
                return Conflict(new { message = "Конфликт голосования. Повторите попытку." });
            }
        }

        [HttpGet("{id}/reviews/ai-summary")]
        [ProducesResponseType(typeof(ReviewSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetReviewAiSummary(int id)
        {
            // 1. Проверка существования товара (опционально)
            var product = await _productService.GetProduct(id); // Или твой метод получения товара
            if (product == null) return NotFound("Product not found");

            var currentReviewCount = await _reviewService.GetReviewCountAsync(id);

            // 2. Пробуем получить из кэша
            var cachedSummary = await _reviewCacheService.GetSummaryAsync(id);
            if (cachedSummary.Summary != null && cachedSummary.ReviewCount == currentReviewCount)
            {
                return Ok(cachedSummary.Summary);
            }

            if (cachedSummary.Summary != null && cachedSummary.ReviewCount != currentReviewCount)
            {
                await _reviewCacheService.InvalidateSummaryAsync(id);
            }

            // 3. Пытаемся захватить блокировку
            var lockTimeout = TimeSpan.FromSeconds(10);
            var isLocked = await _reviewCacheService.TryAcquireLockAsync(id, lockTimeout);

            if (!isLocked)
            {
                return StatusCode(503, "AI analysis is currently being generated. Please try again in a few seconds.");
            }

            try
            {
                currentReviewCount = await _reviewService.GetReviewCountAsync(id);

                // Double-check кэша
                cachedSummary = await _reviewCacheService.GetSummaryAsync(id);
                if (cachedSummary.Summary != null && cachedSummary.ReviewCount == currentReviewCount)
                {
                    return Ok(cachedSummary.Summary);
                }

                // 4. Получаем тексты отзывов ЧЕРЕЗ СЕРВИС (BLL Layer)
                var reviewTexts = await _reviewService.GetRecentReviewTextsAsync(id, count: 50);

                if (!reviewTexts.Any())
                {
                    return NotFound("No reviews found for this product.");
                }

                // 5. Генерируем анализ
                var summary = await _aiAnalysisService.GenerateReviewSummaryAsync(reviewTexts);

                if (summary == null)
                {
                    return StatusCode(500, "Failed to generate AI summary.");
                }

                // 6. Сохраняем в кэш на 24 часа
                await _reviewCacheService.SetSummaryAsync(id, summary, currentReviewCount, TimeSpan.FromHours(24));

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary for product {ProductId}", id);
                return StatusCode(500, "Internal server error during analysis generation.");
            }
            finally
            {
                await _reviewCacheService.ReleaseLockAsync(id);
            }
        }
    }
}
