using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly ICategoryRepository _categoryRepository;
        private readonly int _rndLimit = 12;
        private readonly ILogger<ProductService> _logger;
        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, IMapper mapper, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
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
        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryName(
            string categoryName,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? inStock = null,
            string sortBy = "ProductName",
            string sortOrder = "asc")
        {
            var products = await _productRepository.GetProductsByCategoryNameAsync(
                categoryName,
                minPrice,
                maxPrice,
                inStock,
                sortBy,
                sortOrder);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        public async Task<IEnumerable<ProductDto>> GetRandomProducts()
        {
            var products = await _productRepository.GetProducts();
            var random = new Random();
            var rndProducts = products.OrderBy(x => random.Next()).Take(_rndLimit);
            return _mapper.Map<IEnumerable<ProductDto>>(rndProducts);
        }
        public async Task<List<ProductSpecDto>?> GetProductSpecificationsAsync(int id)
        {
            var rawSpecs = await _productRepository.GetProductSpecificationsAsync(id);

            if (rawSpecs == null)
                return null;

            var specsDtoList = _mapper.Map<List<ProductSpecDto>>(rawSpecs);

            return specsDtoList;
        }

        public async Task<CategorySpecificationFiltersDto?> GetSpecificationFiltersForCategoryAsync(string categoryName)
        {
            var specs = await _productRepository.GetSpecificationsByGroupNameAsync(categoryName);

            if (!specs.Any()) return null; // Категория не найдена или нет фильтров

            var filterDtos = new List<SpecificationFilterDto>();

            foreach (var spec in specs)
            {
                var filterDto = new SpecificationFilterDto
                {
                    SpecId = spec.SpecId,
                    Name = spec.Name,
                    DisplayName = spec.DisplayName,
                    DataType = spec.DataType
                };

                // Опционально: Загрузить возможные значения для UI
                if (spec.DataType == "Text")
                {
                    var (textValues, _) = await _productRepository.GetPossibleValuesForSpecAsync(spec.SpecId); // Исправлен порядок
                    if (textValues != null) // Проверяем на null
                    {
                        filterDto.PossibleValues = textValues.Cast<object>().ToList(); // textValues уже List<string>, Cast не нужен, но для DTO подойдет
                        // ИЛИ напрямую, если DTO ожидает List<string>:
                        // filterDto.PossibleValues = textValues; // Если тип поля в DTO List<string>
                    }
                }
                else if (spec.DataType == "Number")
                {
                    var (_, numberRange) = await _productRepository.GetPossibleValuesForSpecAsync(spec.SpecId); // Исправлен порядок
                    if (numberRange.HasValue) // Проверяем, есть ли диапазон
                    {
                        // numberRange.Value - это кортеж (decimal? Min, decimal? Max)
                        // Проверяем, не равны ли Min/Max null
                        var min = numberRange.Value.Min;
                        var max = numberRange.Value.Max;

                        // Создаем список объектов для DTO, учитывая, что Min/Max могут быть null
                        var rangeList = new List<object>();
                        if (min.HasValue) rangeList.Add(min.Value);
                        if (max.HasValue) rangeList.Add(max.Value);

                        filterDto.PossibleValues = rangeList;
                        // Или, если DTO ожидает отдельные поля Min/Max, добавьте их туда и не трогайте PossibleValues
                    }
                }

                filterDtos.Add(filterDto);
            }

            return new CategorySpecificationFiltersDto
            {
                CategoryName = categoryName,
                Filters = filterDtos
            };
        }


        public async Task<Dictionary<string, object>?> ValidateSpecFiltersAsync(Dictionary<string, object> specFilters, string category)
        {
            // 1. Получить допустимые характеристики для категории
            var validSpecs = await _productRepository.GetSpecificationsByGroupNameAsync(category);

            if (!validSpecs.Any())
            {
                // Логгируем, что группа не найдена или нет фильтров
                _logger.LogWarning("Для категории '{Category}' не найдено допустимых характеристик для фильтрации.", category);
                return null; // Категория не найдена или нет характеристик
            }

            // Логгируем найденные имена характеристик
            var validSpecNames = validSpecs.Select(s => s.Name).ToList();
            _logger.LogDebug("Найдены характеристики для категории '{Category}': {@ValidSpecNames}", category, validSpecNames);

            // 2. Создать словарь для быстрого поиска типа характеристики
            var validSpecTypes = validSpecs.ToDictionary(s => s.Name, s => s.DataType);

            var validatedFilters = new Dictionary<string, object>();

            foreach (var filter in specFilters)
            {
                var specName = filter.Key;
                var specValue = filter.Value;

                // 3. Проверить, является ли имя характеристики допустимым
                if (!validSpecTypes.TryGetValue(specName, out var expectedType))
                {
                    // Логгируем, какая характеристика не найдена
                    _logger.LogWarning("Характеристика '{SpecName}' не найдена или не является фильтруемой для категории '{Category}'.", specName, category);
                    return null; // Характеристика не найдена для категории
                }

                // ... остальная логика проверки типа ...
                // (оставлю как есть, но можно добавить логи и тут)
                if (expectedType == "Number")
                {
                    // Проверить, является ли значение числом или объектом с Min/Max
                    if (specValue is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            decimal? min = null, max = null;
                            bool isValidObject = true;
                            if (element.TryGetProperty("Min", out var minProp))
                            {
                                if (minProp.ValueKind == JsonValueKind.Number)
                                {
                                    min = minProp.GetDecimal();
                                }
                                else
                                {
                                    isValidObject = false;
                                }
                            }
                            if (element.TryGetProperty("Max", out var maxProp))
                            {
                                if (maxProp.ValueKind == JsonValueKind.Number)
                                {
                                    max = maxProp.GetDecimal();
                                }
                                else
                                {
                                    isValidObject = false;
                                }
                            }
                            if (isValidObject && (min.HasValue || max.HasValue))
                            {
                                validatedFilters[specName] = new { Min = min, Max = max };
                                continue;
                            }
                        }
                        else if (element.ValueKind == JsonValueKind.Number)
                        {
                            // Простое числовое значение
                            validatedFilters[specName] = element.GetDecimal();
                            continue;
                        }
                    }
                    else if (specValue is decimal decVal)
                    {
                        validatedFilters[specName] = decVal;
                        continue;
                    }
                    else if (specValue is int intVal)
                    {
                        validatedFilters[specName] = (decimal)intVal;
                        continue;
                    }

                    // Если дошли сюда, тип не подходит
                    _logger.LogWarning("Значение фильтра '{SpecName}' для категории '{Category}' ожидается числовым, получено: {Type} ({Value})", specName, category, specValue.GetType().Name, specValue);
                    return null;
                }
                else if (expectedType == "Text")
                {
                    if (specValue is string strVal)
                    {
                        validatedFilters[specName] = strVal;
                        continue;
                    }
                    // Можно добавить проверку на массив строк для множественного выбора
                    // if (specValue is JsonElement arrEl && arrEl.ValueKind == JsonValueKind.Array) { ... }
                    _logger.LogWarning("Значение фильтра '{SpecName}' для категории '{Category}' ожидается текстовым (string), получено: {Type} ({Value})", specName, category, specValue.GetType().Name, specValue);
                    return null; // Тип не подходит
                }
                else
                {
                    _logger.LogError("Неизвестный тип данных '{DataType}' для характеристики '{SpecName}' в категории '{Category}'.", expectedType, specName, category);
                    return null; // Неизвестный тип
                }
            }

            // 5. Все фильтры валидны
            return validatedFilters;
        }
    }
}
