using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _appDbContext;
        public CategoryRepository(AppDbContext context)
        {
            _appDbContext = context;
        }
        public async Task CreateCategory(Category category)
        {
            await _appDbContext.Categories.AddAsync(category);
            await _appDbContext.SaveChangesAsync();   
        }

        public async Task DeleteCategory(int id)
        {
            var product = await GetCategory(id);
            _appDbContext.Categories.Remove(product);
            await _appDbContext.SaveChangesAsync();
        }

        public async Task<bool> ExistsCategory(int id)
        {
            return await _appDbContext.Categories.AnyAsync(c => c.CategoryId == id);
        }

        public async Task<IEnumerable<Category>> GetCategories()
        {
            return await _appDbContext.Categories.ToListAsync();
        }

        public async Task<Category> GetCategory(int id)
        {
            return await _appDbContext.Categories.FindAsync(id);
        }

        public async Task UpdateCategory(Category category)
        {
            _appDbContext.Update(category);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task<int> GetCategoryByName(string name)
        {
            var category = await _appDbContext.Categories.
                FirstOrDefaultAsync(c => c.CategoryName == name);
            return category.CategoryId;
        }
    }
}
