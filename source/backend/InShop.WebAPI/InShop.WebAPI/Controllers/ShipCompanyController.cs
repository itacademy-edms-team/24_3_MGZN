using Contracts.Dtos;
using InShop.WebAPI.Extensions;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
        public async Task<IActionResult> Add([FromBody] ShipCompanyCreateDto companyDto)
        {
            try
            {
                await _shipCompanyService.AddShipCompany(companyDto);
                return Ok("Новая компания добавлена");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut]
        [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
        public async Task<IActionResult> Update([FromBody] ShipCompanyDto companyDto)
        {
            try
            {
                await _shipCompanyService.UpdateShipCompany(companyDto);
                return Ok("Информация о компании обновлена");
            }
            catch (Exception ex)
            {
                return ToShipCompanyErrorResult(ex);
            }
        }
        [HttpDelete("{id}")]
        [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _shipCompanyService.DeleteShipCompany(id);
                return Ok("Информация о компании удалена");
            }
            catch (Exception ex)
            {
                return ToShipCompanyErrorResult(ex);
            }
        }

        private IActionResult ToShipCompanyErrorResult(Exception ex)
        {
            if (ex.Message.Contains("не найдена", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }
}
