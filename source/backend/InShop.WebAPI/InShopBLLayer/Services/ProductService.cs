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
        private readonly IProductRepository _repository;
        private readonly IMapper _mapper;
        public ProductService(IProductRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ProductDto?> GetProduct(int id)
        {
            var product = await _repository.GetProduct(id);
            return _mapper.Map<ProductDto>(product);
        }
        public async Task<IEnumerable<ProductDto>> GetProducts()
        {
            var products = await _repository.GetProducts();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        public async Task<int> CreateProduct(ProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            await _repository.CreateProduct(product); // Ваш текущий метод
            return product.ProductId; // Возвращаем ID после сохранения
        }
        public async Task DeleteProduct(int id)
        {
            if (!await _repository.ExistsProduct(id))
                throw new Exception("Товар не найден");
            await _repository.DeleteProduct(id);
        }
        public async Task UpdateProduct(ProductDto productDto)
        {
            if (!await _repository.ExistsProduct(productDto.ProductId))
                throw new Exception("Товар не найден");
            var editedProduct = _mapper.Map<Product>(productDto);
            await _repository.UpdateProducts(editedProduct);
        }
    }
}
