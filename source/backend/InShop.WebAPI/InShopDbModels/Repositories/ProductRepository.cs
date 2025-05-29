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
            return await _appDbContext.Products.Include(p => p.ProductCategory).Where(p => p.ProductId == id).FirstOrDefaultAsync();
            //return await _appDbContext.Orders.Include(p => p.OrderItems).ThenInclude(oi => oi.Product).ToListAsync();
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
    }
}