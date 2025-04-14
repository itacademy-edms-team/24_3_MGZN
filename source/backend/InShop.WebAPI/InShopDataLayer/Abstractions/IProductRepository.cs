using InShopDataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDataLayer.Abstractions
{
    public interface  IProductRepository
    {
        Task<List<Product>> GetProducts();
        void DeleteProduct(int id);
        void CreateProduct(Product product);
        void UpdateProducts(int id, string name,
                                 string? description,
                                 decimal price,
                                 bool availability,
                                 int categoryID,
                                 int stockQuantity,
                                 string? imageURL);
    }
}
