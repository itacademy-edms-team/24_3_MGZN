using AutoMapper;
using Contracts.Dtos;
using FluentAssertions;
using InShopBLLayer.Services;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Moq;

namespace InShopBLLayer.Tests.Services;

public class ShipCompanyServiceTests
{
    private readonly Mock<IShipCompanyRepository> _repository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly ShipCompanyService _sut;

    public ShipCompanyServiceTests()
    {
        _sut = new ShipCompanyService(_repository.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetShipCompany_WhenExists_ReturnsMappedDto()
    {
        var company = new ShipCompany { ShipCompanyId = 1, ShipCompanyName = "CDEK" };
        var dto = new ShipCompanyDto { ShipCompanyId = 1, ShipCompanyName = "CDEK" };

        _repository.Setup(r => r.GetShipCompany(1)).ReturnsAsync(company);
        _mapper.Setup(m => m.Map<ShipCompanyDto?>(company)).Returns(dto);

        var result = await _sut.GetShipCompany(1);

        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetShipCompanies_ReturnsMappedList()
    {
        var companies = new List<ShipCompany> { new() { ShipCompanyId = 1, ShipCompanyName = "CDEK" } };
        var dtos = new List<ShipCompanyDto> { new() { ShipCompanyId = 1, ShipCompanyName = "CDEK" } };

        _repository.Setup(r => r.GetShipCompanies()).ReturnsAsync(companies);
        _mapper.Setup(m => m.Map<IEnumerable<ShipCompanyDto>>(companies)).Returns(dtos);

        var result = await _sut.GetShipCompanies();

        result.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task DeleteShipCompany_WhenMissing_Throws()
    {
        _repository.Setup(r => r.ExistsShipCompany(99)).ReturnsAsync(false);

        var act = () => _sut.DeleteShipCompany(99);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*не найдена*");
    }

    [Fact]
    public async Task UpdateShipCompany_WhenMissing_Throws()
    {
        var dto = new ShipCompanyDto { ShipCompanyId = 3, ShipCompanyName = "X" };
        _repository.Setup(r => r.ExistsShipCompany(3)).ReturnsAsync(false);

        var act = () => _sut.UpdateShipCompany(dto);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*не найдена*");
    }

    [Fact]
    public async Task AddShipCompany_MapsAndPersists()
    {
        var createDto = new ShipCompanyCreateDto { ShipCompanyName = "Почта" };
        var entity = new ShipCompany { ShipCompanyName = "Почта" };

        _mapper.Setup(m => m.Map<ShipCompany>(createDto)).Returns(entity);

        await _sut.AddShipCompany(createDto);

        _repository.Verify(r => r.AddShipCompany(entity), Times.Once);
    }
}
