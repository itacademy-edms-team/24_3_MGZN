using Contracts.Dtos.ProductsDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetProducts();
        Task<ProductDto?> GetProduct(int id);
        Task CreateProduct(ProductCreateDto productDto);
        Task UpdateProduct(ProductDto productDto);
        Task DeleteProduct(int id);
    }
}
