using InShopDataLayer.Abstractions;
using InShopDataLayer.Data;
using InShopDataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDataLayer.Repositories
{
    internal class ProductRepository: IProductRepository
    {
        private readonly AppDbContext _appDbContext;
        public ProductRepository(AppDbContext context)
        {
            _appDbContext = context;
        }

        public async Task<List<Product>> GetProducts()
        {
            return await _appDbContext.Products
                         .AsNoTracking()
                         .ToList();
        }
        public async void DeleteProduct(int id)
        {
            var product = await  _appDbContext.Products.FindAsync(id);

            if (product == null)
            {
                throw new Exception("Товар не найден");
            }

            _appDbContext.Products.Remove(product);
            await _appDbContext.SaveChangesAsync();
        }
        public async void CreateProduct(Product product)
        {
            await _appDbContext.Products.AddAsync(product);
            await _appDbContext.SaveChangesAsync();
        }
    }
}
