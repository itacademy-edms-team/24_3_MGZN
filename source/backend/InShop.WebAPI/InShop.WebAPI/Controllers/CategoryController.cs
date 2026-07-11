using Contracts.Dtos;
using InShop.WebAPI.Extensions;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService; 
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var category = await _categoryService.GetCategory(id);
            return category == null ? NotFound() : Ok(category);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetCategories();
            return categories == null ? NotFound(): Ok(categories);
        }
        [HttpPut]
        [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
        public async Task<IActionResult> Update([FromBody] CategoryDto categoryDto)
        {
            try
            {
                await _categoryService.UpdateCategory(categoryDto);
                return Ok("Информация о категории обновлена");
            }
            catch (Exception ex)
            {
                return ToCategoryErrorResult(ex);
            }
        }
        [HttpPost]
        [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto categoryDto)
        {
            try
            {
                await _categoryService.CreateCategory(categoryDto);
                return Ok("Новая категория добавлена");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _categoryService.DeleteCategory(id);
                return Ok("Информация о категории удалена");
            }
            catch (Exception ex)
            {
                return ToCategoryErrorResult(ex);
            }
        }
        [HttpGet("categoryName")]
        public async Task<IActionResult> GetCategoryByName([FromQuery] string categoryName)
        {
            var category = await _categoryService.GetCategoryByName(categoryName);
            return Ok(category);
        }

        private IActionResult ToCategoryErrorResult(Exception ex)
        {
            if (ex.Message.Contains("не найдена", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }
}
