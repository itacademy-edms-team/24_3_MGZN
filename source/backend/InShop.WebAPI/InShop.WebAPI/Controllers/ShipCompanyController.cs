using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipCompanyController : ControllerBase
    {
        private readonly IShipCompanyService _shipCompanyService;
        public ShipCompanyController(IShipCompanyService shipCompanyService)
        {
            _shipCompanyService = shipCompanyService;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var company = await _shipCompanyService.GetShipCompany(id);
            return company == null ? NotFound() : Ok(company);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companies = await _shipCompanyService.GetShipCompanies();
            return companies == null ? NotFound() : Ok(companies);
        }
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] ShipCompanyCreateDto companyDto)
        {
            await _shipCompanyService.AddShipCompany(companyDto);
            return Ok("Новая компания добавлена");
        }
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ShipCompanyDto companyDto)
        {
            await _shipCompanyService.UpdateShipCompany(companyDto);
            return Ok("Информация о компании обновлена");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _shipCompanyService.DeleteShipCompany(id);
            return Ok("Информация о компании удалена");
        }
    }
}
