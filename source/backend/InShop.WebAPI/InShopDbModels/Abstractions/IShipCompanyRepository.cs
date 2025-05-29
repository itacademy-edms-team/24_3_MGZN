using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IShipCompanyRepository
    {
        Task<IEnumerable<ShipCompany>> GetShipCompanies();
        Task<ShipCompany> GetShipCompany(int id);
        Task AddShipCompany(ShipCompany company);
        Task DeleteShipCompany(int id);
        Task UpdateShipCompany(ShipCompany company);
        Task<bool> ExistsShipCompany(int id);
    }
}
