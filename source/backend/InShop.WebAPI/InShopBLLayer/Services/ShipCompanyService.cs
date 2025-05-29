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
    public class ShipCompanyService : IShipCompanyService
    {
        private readonly IShipCompanyRepository _repository;
        private readonly IMapper _mapper;
        public ShipCompanyService(IShipCompanyRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<IEnumerable<ShipCompanyDto>> GetShipCompanies()
        {
            var companies = await _repository.GetShipCompanies();
            return _mapper.Map<IEnumerable<ShipCompanyDto>>(companies);
        }
        public async Task<ShipCompanyDto?> GetShipCompany(int id)
        {
            var company = await _repository.GetShipCompany(id);
            return _mapper.Map<ShipCompanyDto?>(company);
        }
        public async Task AddShipCompany(ShipCompanyCreateDto companyDto)
        {
            var category = _mapper.Map<ShipCompany>(companyDto);
            await _repository.AddShipCompany(category); 
        }
        public async Task DeleteShipCompany(int id)
        {
            if (!await _repository.ExistsShipCompany(id))
                throw new Exception("Компания не найдена");
            await _repository.DeleteShipCompany(id);
        }
        public async Task UpdateShipCompany(ShipCompanyDto companyDto)
        {
            if (!await _repository.ExistsShipCompany(companyDto.ShipCompanyId))
                throw new Exception("Компания не найдена");
            var company = _mapper.Map<ShipCompany>(companyDto);
            await _repository.UpdateShipCompany(company);
        }
    }
}
