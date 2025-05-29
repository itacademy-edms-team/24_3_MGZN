using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetCategories();
        Task<Category> GetCategory(int id);
        Task CreateCategory(Category category);
        Task DeleteCategory(int id);
        Task UpdateCategory(Category category);
        Task<bool> ExistsCategory(int id);
        Task<int> GetCategoryByName(string name);
    }
}
