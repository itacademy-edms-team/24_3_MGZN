using Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IShipCompanyService
    {
        Task<IEnumerable<ShipCompanyDto>> GetShipCompanies();
        Task<ShipCompanyDto?> GetShipCompany(int id);
        Task DeleteShipCompany(int id);
        Task UpdateShipCompany(ShipCompanyDto shipCompany);
        Task AddShipCompany(ShipCompanyCreateDto shipCompany);
    }
}
