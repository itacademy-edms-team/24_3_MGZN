    using InShopBLLayer.Abstractions;
using InShopBLLayer.Services;
using Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductsController(IProductService productService)
        {
            _productService = productService;
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
        [HttpGet("products-by-category")] // GET /api/Products/products-by-category?categoryName=...&sortBy=...&sortOrder=...
        public async Task<IActionResult> GetProductsByCategory(
                [FromQuery] string categoryName,
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

                var productsByCategory = await _productService.GetProductsByCategoryName(categoryName, sortBy, sortOrder);
                return Ok(productsByCategory);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
