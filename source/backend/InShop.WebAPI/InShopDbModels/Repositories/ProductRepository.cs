using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _appDbContext;
        public ProductRepository(AppDbContext context)
        {
            _appDbContext = context;
        }

        public async Task<IEnumerable<Product>> GetProducts()
        {
            return await _appDbContext.Products
                         .ToListAsync();
        }
        public async Task<Product> GetProduct(int id)
        {
            return await _appDbContext.Products
                                .Include(p => p.ProductCategory)
                                .Where(p => p.ProductId == id)
                                .FirstOrDefaultAsync();
        }
        public async Task DeleteProduct(int id)
        {
            var product = await GetProduct(id);
            _appDbContext.Products.Remove(product);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task CreateProduct(Product product)
        {
            await _appDbContext.Products.AddAsync(product);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task UpdateProduct(Product product)
        {
            _appDbContext.Products.Update(product);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task<bool> ExistsProduct(int id)
        {
            return await _appDbContext.Products.AnyAsync(p => p.ProductId == id);
        }
        public async Task<IEnumerable<Product>> GetProductsByCategoryId(int categoryId)
        {
            return await _appDbContext.Products
                .Where(p => p.ProductCategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryNameAsync(string categoryName, string sortBy, string sortOrder)
        {
            Console.WriteLine($"DEBUG REPO: Input categoryName: '{categoryName}', Type: {categoryName.GetType()}, Length: {categoryName.Length}");
            Console.WriteLine($"DEBUG REPO: Input categoryName Bytes: [{string.Join(", ", System.Text.Encoding.UTF8.GetBytes(categoryName))}]"); // Проверим байты

            // Запрос категории
            var categoryQuery = _appDbContext.Categories.Where(c => c.CategoryName == categoryName);
            var sql = categoryQuery.ToQueryString(); // EF Core 5+
            Console.WriteLine($"DEBUG REPO: Category SQL Query: {sql}");

            var categoryId = await categoryQuery
                .Select(c => c.CategoryId)
                .FirstOrDefaultAsync();

            Console.WriteLine($"DEBUG REPO: Query result categoryId: {categoryId}"); // <-- Вот это значение

            if (categoryId == 0)
            {
                Console.WriteLine("DEBUG REPO: Category not found or CategoryId is 0. Fetching all categories for comparison.");
                var allCategories = await _appDbContext.Categories.ToListAsync();
                foreach (var cat in allCategories)
                {
                    Console.WriteLine($"DEBUG REPO: DB CategoryName: '{cat.CategoryName}', CategoryId: {cat.CategoryId}, Length: {cat.CategoryName.Length}");
                    Console.WriteLine($"DEBUG REPO: DB CategoryName Bytes: [{string.Join(", ", System.Text.Encoding.UTF8.GetBytes(cat.CategoryName))}]");
                }
                return Enumerable.Empty<Product>();
            }

            // Запрос продуктов
            IQueryable<Product> query = _appDbContext.Products
                .Where(p => p.ProductCategoryId == categoryId);

            query = sortBy.ToLower() switch
            {
                "productname" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.ProductName) : query.OrderBy(p => p.ProductName),
                "price" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.ProductPrice) : query.OrderBy(p => p.ProductPrice),
                _ => query.OrderBy(p => p.ProductName)
            };

            var products = await query.ToListAsync();
            Console.WriteLine($"DEBUG REPO: Found {products.Count} products for categoryId {categoryId}");
            return products;
        }
    }
}