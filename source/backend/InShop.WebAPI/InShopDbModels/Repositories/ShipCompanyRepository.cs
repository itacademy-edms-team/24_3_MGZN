using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Repositories
{
    public class ShipCompanyRepository: IShipCompanyRepository
    {
        private readonly AppDbContext _appDbContext;
        public ShipCompanyRepository(AppDbContext context)
        {
            _appDbContext = context;
        }
        public async Task<IEnumerable<ShipCompany>> GetShipCompanies()
        {
            return await _appDbContext.ShipCompanies.ToListAsync();
        }
        public async Task<ShipCompany> GetShipCompany(int id)
        {
            return await _appDbContext.ShipCompanies.FindAsync(id);
        }
        public async Task AddShipCompany(ShipCompany company)
        {
            await _appDbContext.ShipCompanies.AddAsync(company);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task DeleteShipCompany(int id)
        {
            var company = await GetShipCompany(id);
            _appDbContext.ShipCompanies.Remove(company);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task UpdateShipCompany(ShipCompany company)
        {
            _appDbContext.ShipCompanies.Update(company);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task<bool> ExistsShipCompany(int id)
        {
            return await _appDbContext.ShipCompanies.AnyAsync(c => c.ShipCompanyId == id);
        }
    }
}
