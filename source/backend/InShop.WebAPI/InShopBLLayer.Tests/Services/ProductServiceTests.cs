using System.Text.Json;
using AutoMapper;
using Contracts.Dtos;
using FluentAssertions;
using InShopBLLayer.Services;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InShopBLLayer.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _productRepository.Object,
            _categoryRepository.Object,
            _mapper.Object,
            NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task GetProduct_WhenExists_ReturnsMappedDto()
    {
        var product = new Product { ProductId = 1, ProductName = "Laptop" };
        var dto = new ProductDto { ProductId = 1, ProductName = "Laptop" };

        _productRepository.Setup(r => r.GetProduct(1)).ReturnsAsync(product);
        _mapper.Setup(m => m.Map<ProductDto>(product)).Returns(dto);

        var result = await _sut.GetProduct(1);

        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task DeleteProduct_WhenMissing_Throws()
    {
        _productRepository.Setup(r => r.ExistsProduct(99)).ReturnsAsync(false);

        var act = () => _sut.DeleteProduct(99);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*не найден*");
    }

    [Fact]
    public async Task UpdateProduct_WhenMissing_Throws()
    {
        var dto = new ProductDto { ProductId = 5, ProductName = "X" };
        _productRepository.Setup(r => r.ExistsProduct(5)).ReturnsAsync(false);

        var act = () => _sut.UpdateProduct(dto);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*не найден*");
    }

    [Fact]
    public async Task GetProducts_ReturnsMappedList()
    {
        var products = new List<Product> { new() { ProductId = 1, ProductName = "A" } };
        var dtos = new List<ProductDto> { new() { ProductId = 1, ProductName = "A" } };

        _productRepository.Setup(r => r.GetProducts()).ReturnsAsync(products);
        _mapper.Setup(m => m.Map<IEnumerable<ProductDto>>(products)).Returns(dtos);

        var result = await _sut.GetProducts();

        result.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task GetProductSpecificationsAsync_WhenNull_ReturnsNull()
    {
        _productRepository.Setup(r => r.GetProductSpecificationsAsync(1))
            .ReturnsAsync((List<(int, string, string, string, string?, decimal?)>?)null);

        var result = await _sut.GetProductSpecificationsAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSpecificationFiltersForCategoryAsync_WhenNoSpecs_ReturnsNull()
    {
        _productRepository.Setup(r => r.GetSpecificationsByGroupNameAsync("unknown"))
            .ReturnsAsync(new List<ProductSpecification>());

        var result = await _sut.GetSpecificationFiltersForCategoryAsync("unknown");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSpecificationFiltersForCategoryAsync_WhenTextSpec_ReturnsFilterWithValues()
    {
        var specs = new List<ProductSpecification>
        {
            new() { SpecId = 1, Name = "brand", DisplayName = "Бренд", DataType = "Text" }
        };

        _productRepository.Setup(r => r.GetSpecificationsByGroupNameAsync("laptops")).ReturnsAsync(specs);
        _productRepository.Setup(r => r.GetPossibleValuesForSpecAsync(1))
            .ReturnsAsync((new List<string> { "Dell", "HP" }, null));

        var result = await _sut.GetSpecificationFiltersForCategoryAsync("laptops");

        result.Should().NotBeNull();
        result!.CategoryName.Should().Be("laptops");
        result.Filters.Should().ContainSingle(f => f.Name == "brand");
    }

    [Fact]
    public async Task ValidateSpecFiltersAsync_WhenCategoryHasNoSpecs_ReturnsNull()
    {
        _productRepository.Setup(r => r.GetSpecificationsByGroupNameAsync("empty"))
            .ReturnsAsync(new List<ProductSpecification>());

        var result = await _sut.ValidateSpecFiltersAsync(new Dictionary<string, object>(), "empty");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateSpecFiltersAsync_WhenUnknownSpec_ReturnsNull()
    {
        var specs = new List<ProductSpecification>
        {
            new() { SpecId = 1, Name = "brand", DisplayName = "Бренд", DataType = "Text" }
        };
        _productRepository.Setup(r => r.GetSpecificationsByGroupNameAsync("laptops")).ReturnsAsync(specs);

        var filters = new Dictionary<string, object> { ["unknown"] = "value" };
        var result = await _sut.ValidateSpecFiltersAsync(filters, "laptops");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateSpecFiltersAsync_WhenValidTextFilter_ReturnsValidated()
    {
        var specs = new List<ProductSpecification>
        {
            new() { SpecId = 1, Name = "brand", DisplayName = "Бренд", DataType = "Text" }
        };
        _productRepository.Setup(r => r.GetSpecificationsByGroupNameAsync("laptops")).ReturnsAsync(specs);

        var filters = new Dictionary<string, object> { ["brand"] = "Dell" };
        var result = await _sut.ValidateSpecFiltersAsync(filters, "laptops");

        result.Should().NotBeNull();
        result!["brand"].Should().Be("Dell");
    }

    [Fact]
    public async Task ValidateSpecFiltersAsync_WhenValidNumberFilter_ReturnsValidated()
    {
        var specs = new List<ProductSpecification>
        {
            new() { SpecId = 2, Name = "price", DisplayName = "Цена", DataType = "Number" }
        };
        _productRepository.Setup(r => r.GetSpecificationsByGroupNameAsync("laptops")).ReturnsAsync(specs);

        var filters = new Dictionary<string, object> { ["price"] = 50000m };
        var result = await _sut.ValidateSpecFiltersAsync(filters, "laptops");

        result.Should().NotBeNull();
        result!["price"].Should().Be(50000m);
    }

    [Fact]
    public async Task ValidateSpecFiltersAsync_WhenTextFromJsonElement_ReturnsValidated()
    {
        var specs = new List<ProductSpecification>
        {
            new() { SpecId = 1, Name = "brand", DisplayName = "Бренд", DataType = "Text" }
        };
        _productRepository.Setup(r => r.GetSpecificationsByGroupNameAsync("laptops")).ReturnsAsync(specs);

        using var doc = JsonDocument.Parse("\"Dell\"");
        var filters = new Dictionary<string, object> { ["brand"] = doc.RootElement.Clone() };

        var result = await _sut.ValidateSpecFiltersAsync(filters, "laptops");

        result.Should().NotBeNull();
        result!["brand"].Should().Be("Dell");
    }

    [Fact]
    public async Task GetRandomProducts_ReturnsAtMostTwelveItems()
    {
        var products = Enumerable.Range(1, 20)
            .Select(i => new Product { ProductId = i, ProductName = $"P{i}" })
            .ToList();
        var dtos = products.Select(p => new ProductDto { ProductId = p.ProductId, ProductName = p.ProductName }).ToList();

        _productRepository.Setup(r => r.GetProducts()).ReturnsAsync(products);
        _mapper.Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns((IEnumerable<Product> src) => src.Select(p => new ProductDto { ProductId = p.ProductId }));

        var result = await _sut.GetRandomProducts();

        result.Should().HaveCountLessThanOrEqualTo(12);
    }
}
