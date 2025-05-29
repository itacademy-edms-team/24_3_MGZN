using Contracts.Dtos;
using InShopBLLayer.Abstractions;
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
        public async Task<IActionResult> Update([FromBody] CategoryDto categoryDto)
        {
            await _categoryService.UpdateCategory(categoryDto);
            return Ok("Информация о категории обновлена");
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto categoryDto)
        {
            await _categoryService.CreateCategory(categoryDto);
            return Ok("Новая категория добавлена");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteCategory(id);
            return Ok("Информация о категории удалена");
        }
    }
}
