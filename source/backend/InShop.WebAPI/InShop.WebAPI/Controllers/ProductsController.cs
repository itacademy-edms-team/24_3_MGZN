using InShopBLLayer.Abstractions;
using InShopBLLayer.Services;
using Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;
using InShop.WebAPI.Extensions;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        public ProductsController(IProductService productService, IReviewService reviewService)
        {
            _productService = productService;
            _reviewService = reviewService;
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
        }
    }
}
