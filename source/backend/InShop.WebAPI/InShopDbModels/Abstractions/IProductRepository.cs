using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetProducts();
        Task<Product> GetProduct(int id);
        Task DeleteProduct(int id);
        Task CreateProduct(Product product);
        Task UpdateProduct(Product product);
        Task<bool> ExistsProduct(int id);
    }
}
