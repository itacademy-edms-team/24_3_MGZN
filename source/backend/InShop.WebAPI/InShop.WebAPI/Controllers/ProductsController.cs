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
        [HttpGet("products-by-category")]
        public async Task<IActionResult> GetProductsByCategory([FromQuery] string categoryName)
        {
            try
            {
                var productsByCategory = await _productService.GetProductsByCategoryName(categoryName);
                return Ok(productsByCategory);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
