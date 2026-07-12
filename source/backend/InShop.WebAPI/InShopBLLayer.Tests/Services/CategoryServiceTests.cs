using AutoMapper;
using Contracts.Dtos;
using FluentAssertions;
using InShopBLLayer.Services;
using InShopBLLayer.Services.Admin;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace InShopBLLayer.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "inshop-category-tests"));
        var imageStorage = new ProductImageStorage(env.Object);
        _sut = new CategoryService(_repository.Object, _mapper.Object, imageStorage);
    }

    [Fact]
    public async Task GetCategory_WhenExists_ReturnsMappedDto()
    {
        var category = new Category { CategoryId = 1, CategoryName = "Аудио" };
        var dto = new CategoryDto { CategoryId = 1, CategoryName = "Аудио" };

        _repository.Setup(r => r.GetCategory(1)).ReturnsAsync(category);
        _mapper.Setup(m => m.Map<CategoryDto?>(category)).Returns(dto);

        var result = await _sut.GetCategory(1);

        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task DeleteCategory_WhenMissing_Throws()
    {
        _repository.Setup(r => r.ExistsCategory(99)).ReturnsAsync(false);

        var act = () => _sut.DeleteCategory(99);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*не найдена*");
    }

    [Fact]
    public async Task GetCategories_ReturnsMappedList()
    {
        var categories = new List<Category> { new() { CategoryId = 1, CategoryName = "A" } };
        var dtos = new List<CategoryDto> { new() { CategoryId = 1, CategoryName = "A" } };

        _repository.Setup(r => r.GetCategories()).ReturnsAsync(categories);
        _mapper.Setup(m => m.Map<IEnumerable<CategoryDto>>(categories)).Returns(dtos);

        var result = await _sut.GetCategories();

        result.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task UpdateCategory_WhenRemoveImage_ClearsImageUrl()
    {
        var category = new Category
        {
            CategoryId = 1,
            CategoryName = "Аудио",
            ImageUrl = "/images/audio.jpg"
        };

        _repository.Setup(r => r.ExistsCategory(1)).ReturnsAsync(true);
        _repository.Setup(r => r.GetCategory(1)).ReturnsAsync(category);

        await _sut.UpdateCategory(new CategoryDto
        {
            CategoryId = 1,
            CategoryName = "Аудио",
            RemoveImage = true
        });

        category.ImageUrl.Should().BeNull();
        _repository.Verify(r => r.UpdateCategory(category), Times.Once);
    }
}
