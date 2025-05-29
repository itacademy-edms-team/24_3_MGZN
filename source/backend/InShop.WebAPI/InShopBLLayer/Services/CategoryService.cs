using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;
        private readonly IMapper _mapper;
        public CategoryService(ICategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;

        }

        public async Task CreateCategory(CategoryCreateDto categoryDto)
        {
            var category = _mapper.Map<Category>(categoryDto);
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
            var editedCategory = _mapper.Map<Category>(categoryDto);
            await _repository.UpdateCategory(editedCategory);
        }
    }
}
