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
        //CRUD
        Task<IEnumerable<Category>> GetCategories();
        Task<Category> GetById(int id);
        Task AddCategory(Category newCategory);
        Task DeleteCategory(int id);
        Task UpdateCategory(Category category);
    }
}
