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
        public async Task<Product?> GetProduct(int id)
        {
            return await _appDbContext.Products
                                .Include(p => p.ProductCategory)
                                .Where(p => p.ProductId == id)
                                .FirstOrDefaultAsync();
        }
        public async Task DeleteProduct(int id)
        {
            var product = await GetProduct(id);
            if (product is null)
            {
                return;
            }

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

        public async Task<IEnumerable<Product>> GetInStockProductsWithCategoryAsync()
        {
            return await _appDbContext.Products
                .AsNoTracking()
                .Include(p => p.ProductCategory)
                .Where(p => p.ProductAvailability && p.ProductStockQuantity > 0)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryNameAsync(
            string categoryName,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock,
            string sortBy,
            string sortOrder)
        {
            var categoryQuery = _appDbContext.Categories.Where(c => c.CategoryName == categoryName);

            var categoryId = await categoryQuery
                .Select(c => c.CategoryId)
                .FirstOrDefaultAsync();

            if (categoryId == 0)
            {
                return Enumerable.Empty<Product>();
            }

            // Запрос продуктов с фильтрацией по категории
            IQueryable<Product> query = _appDbContext.Products
                .Where(p => p.ProductCategoryId == categoryId);

            // Применяем фильтр по минимальной цене
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice >= minPrice.Value);
            }

            // Применяем фильтр по максимальной цене
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice <= maxPrice.Value);
            }

            // Применяем фильтр по наличию
            if (inStock.HasValue)
            {
                if (inStock.Value)
                {
                    // Только товары в наличии (stock > 0)
                    query = query.Where(p => p.ProductStockQuantity > 0);
                }
                else
                {
                    // Только товары не в наличии (stock = 0)
                    query = query.Where(p => p.ProductStockQuantity == 0);
                }
            }

            // Применяем сортировку
            query = sortBy.ToLower() switch
            {
                "productname" => sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.ProductName)
                    : query.OrderBy(p => p.ProductName),
                "price" => sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.ProductPrice)
                    : query.OrderBy(p => p.ProductPrice),
                _ => query.OrderBy(p => p.ProductName)
            };

            return await query.ToListAsync();
        }

        public async Task<List<(int SpecId, string Name, string DisplayName, string DataType, string? TextValue, decimal? NumberValue)>?> GetProductSpecificationsAsync(int id)
        {
            var exists = await ExistsProduct(id);
            if (!exists)
                return null;

            var productWithSpecs = await _appDbContext.Products
                .Include(p => p.ProductSpecLinks)
                    .ThenInclude(link => link.Spec)
                    .ThenInclude(spec => spec.ProductSpecValues)
                .Include(p => p.ProductSpecLinks)
                    .ThenInclude(link => link.Value)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (productWithSpecs == null || productWithSpecs.ProductSpecLinks == null)
                return new List<(int, string, string, string, string?, decimal?)>();

            var specs = productWithSpecs.ProductSpecLinks
                .Select(link => (
                    SpecId: link.Spec.SpecId,
                    Name: link.Spec.Name,
                    DisplayName: link.Spec.DisplayName,
                    DataType: link.Spec.DataType,
                    TextValue: link.Value.TextValue,
                    NumberValue: link.Value.NumberValue
                ))
                .ToList();

            return specs;
        }
        public async Task<List<ProductSpecification>> GetSpecificationsByGroupNameAsync(string groupName)
        {
            // 1. Найти GroupId по имени группы
            var groupId = await _appDbContext.ProductSpecGroups
                .Where(g => g.CategoryName == groupName)
                .Select(g => g.GroupId)
                .FirstOrDefaultAsync();

            if (groupId == 0) return new List<ProductSpecification>(); // Группа не найдена

            // 2. Найти все характеристики для этой группы
            return await _appDbContext.ProductSpecifications
                .Where(s => s.GroupId == groupId && s.IsFilterable == true) // Только фильтруемые
                .ToListAsync();
        }
        public async Task<(List<string>? TextValues, (decimal? Min, decimal? Max)? NumberRange)> GetPossibleValuesForSpecAsync(int specId)
        {
            // 1. Определить тип характеристики
            var dataType = await _appDbContext.ProductSpecifications
                .Where(s => s.SpecId == specId)
                .Select(s => s.DataType) // "Text" или "Number"
                .FirstOrDefaultAsync();

            if (dataType == "Text")
            {
                // 2. Получить уникальные текстовые значения для этой характеристики
                var values = await _appDbContext.ProductSpecValues
                    .Where(v => v.SpecId == specId && v.TextValue != null) // Исключить null
                    .Select(v => v.TextValue)
                    .Distinct()
                    .ToListAsync();

                return (values, null); // Возвращаем список значений, NumberRange = null
            }
            else if (dataType == "Number")
            {
                // 3. Получить Min/Max для числовой характеристики
                var query = _appDbContext.ProductSpecValues
                    .Where(v => v.SpecId == specId && v.NumberValue != null) // Исключить null
                    .Select(v => v.NumberValue.Value); // Выбираем значение, уже зная, что оно не null

                var min = await query.MinAsync();
                var max = await query.MaxAsync();

                return (null, (min, max)); // Возвращаем null для TextValues, кортеж Min/Max для NumberRange
            }

            // Неизвестный тип или характеристика не найдена
            return (null, null);
        }

        public async Task<List<(int ProductId, string Name, string DisplayName, string? ValueText, decimal? ValueNumber)>> GetAllProductSpecificationsRawAsync(CancellationToken ct)
        {
            var links = await _appDbContext.ProductSpecLinks
                .Include(l => l.Spec)
                .Include(l => l.Value)
                .ToListAsync(ct);

            return links.Select(link => (
                ProductId: link.ProductId,
                Name: link.Spec.Name,
                DisplayName: link.Spec.DisplayName,
                ValueText: link.Value.TextValue,
                ValueNumber: link.Value.NumberValue
            )).ToList();
        }
        public async Task<(decimal AverageRating, int Count)> GetReviewStatsAsync(int productId)
        {
            var stats = await _appDbContext.ProductReviews
                .Where(r => r.ProductId == productId)
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    Avg = g.Average(x => (decimal)x.Rating),
                    Count = g.Count()
                })
                .FirstOrDefaultAsync();

            return (stats?.Avg ?? 0, stats?.Count ?? 0);
        }
    }
}