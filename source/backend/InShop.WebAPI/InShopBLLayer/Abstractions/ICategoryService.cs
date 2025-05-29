using Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetCategories();
        Task<CategoryDto?> GetCategory(int id);
        Task CreateCategory(CategoryCreateDto categoryDto);
        Task DeleteCategory(int id);
        Task UpdateCategory(CategoryDto categoryDto);
    }
}
