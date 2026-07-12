using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Services.Admin;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;

namespace InShopBLLayer.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;
        private readonly IMapper _mapper;
        private readonly ProductImageStorage _imageStorage;

        public CategoryService(
            ICategoryRepository repository,
            IMapper mapper,
            ProductImageStorage imageStorage)
        {
            _repository = repository;
            _mapper = mapper;
            _imageStorage = imageStorage;
        }

        public async Task CreateCategory(CategoryCreateDto categoryDto)
        {
            var category = _mapper.Map<Category>(categoryDto);

            if (!string.IsNullOrWhiteSpace(categoryDto.ImageBase64))
            {
                category.ImageUrl = await _imageStorage.SaveBase64ImageAsync(
                    categoryDto.ImageBase64,
                    ProductImageStorage.CategoriesSubFolder);
            }

            await _repository.CreateCategory(category);
        }

        public async Task DeleteCategory(int id)
        {
            if (!await _repository.ExistsCategory(id))
                throw new Exception("Категория не найдена");
            await _repository.DeleteCategory(id);
        }

        public async Task<IEnumerable<CategoryDto>> GetCategories()
        {
            var categories = await _repository.GetCategories();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto?> GetCategory(int id)
        {
            var category = await _repository.GetCategory(id);
            return _mapper.Map<CategoryDto?>(category);
        }

        public async Task UpdateCategory(CategoryDto categoryDto)
        {
            if (!await _repository.ExistsCategory(categoryDto.CategoryId))
                throw new Exception("Категория не найдена");

            var category = await _repository.GetCategory(categoryDto.CategoryId)
                ?? throw new Exception("Категория не найдена");

            category.CategoryName = categoryDto.CategoryName;

            if (!string.IsNullOrWhiteSpace(categoryDto.ImageBase64))
            {
                var previousUrl = category.ImageUrl;
                category.ImageUrl = await _imageStorage.SaveBase64ImageAsync(
                    categoryDto.ImageBase64,
                    ProductImageStorage.CategoriesSubFolder);
                _imageStorage.TryDeleteImageFile(previousUrl, ProductImageStorage.CategoriesSubFolder);
            }
            else if (categoryDto.RemoveImage)
            {
                _imageStorage.TryDeleteImageFile(category.ImageUrl, ProductImageStorage.CategoriesSubFolder);
                category.ImageUrl = null;
            }
            else if (!string.IsNullOrWhiteSpace(categoryDto.ImageURL))
            {
                category.ImageUrl = categoryDto.ImageURL;
            }

            await _repository.UpdateCategory(category);
        }

        public async Task<CategoryDto> GetCategoryByName(string categoryName)
        {
            var categoryId = await _repository.GetCategoryByName(categoryName);
            var category = await _repository.GetCategory(categoryId);
            if (category == null)
                throw new Exception("Категория не найдена");
            return _mapper.Map<CategoryDto>(category);
        }
    }
}
