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
        public async Task<IActionResult> Create([FromBody] ProductDto product)
        {
            // Cервис возвращает ID созданного продукта
            var productId = await _productService.CreateProduct(product);

            return CreatedAtAction(
                nameof(Get),
                new { id = productId },
                new { Id = productId }); // Возвращаем только ID
        }
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProductDto product)
        {
            await _productService.UpdateProduct(product);
            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProduct(id);
            return NoContent();
        } 

    }
}
