using Contracts.Admin.Dto;
using InShop.WebAPI.Extensions;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers.Admin
{
    [ApiController]
    [Route("api/Admin/products")]
    [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
    public class AdminProductsController : ControllerBase
    {
        private readonly IAdminProductService _productService;

        public AdminProductsController(IAdminProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<AdminProductDto>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _productService.GetProductsAsync(page, pageSize, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminProductDto>> GetProduct(int id, CancellationToken ct)
        {
            var product = await _productService.GetProductAsync(id, ct);
            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<AdminProductDto>> CreateProduct(
            [FromBody] AdminProductCreateDto dto,
            CancellationToken ct)
        {
            try
            {
                var created = await _productService.CreateProductAsync(dto, ct);
                return CreatedAtAction(nameof(GetProduct), new { id = created.ProductId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AdminProductDto>> UpdateProduct(
            int id,
            [FromBody] AdminProductUpdateDto dto,
            CancellationToken ct)
        {
            try
            {
                var updated = await _productService.UpdateProductAsync(id, dto, ct);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
        {
            try
            {
                await _productService.DeleteProductAsync(id, ct);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
