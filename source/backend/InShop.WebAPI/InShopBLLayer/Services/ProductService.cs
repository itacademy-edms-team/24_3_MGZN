using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly ICategoryRepository _categoryRepository;
        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<ProductDto?> GetProduct(int id)
        {
            var product = await _productRepository.GetProduct(id);
            return _mapper.Map<ProductDto>(product);
        }
        public async Task<IEnumerable<ProductDto>> GetProducts()
        {
            var products = await _productRepository.GetProducts();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        public async Task CreateProduct(ProductCreateDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            await _productRepository.CreateProduct(product); 
        }
        public async Task DeleteProduct(int id)
        {
            if (!await _productRepository.ExistsProduct(id))
                throw new Exception("Товар не найден");
            await _productRepository.DeleteProduct(id);
        }
        public async Task UpdateProduct(ProductDto productDto)
        {
            if (!await _productRepository.ExistsProduct(productDto.ProductId))
                throw new Exception("Товар не найден");
            var editedProduct = _mapper.Map<Product>(productDto);
            await _productRepository.UpdateProduct(editedProduct);
        }
        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryName(string categoryName, string sortBy = "ProductName", string sortOrder = "asc")
        {
            var products = await _productRepository.GetProductsByCategoryNameAsync(categoryName, sortBy, sortOrder);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
    }
}
