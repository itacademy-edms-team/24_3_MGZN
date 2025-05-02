using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IAdminRepository
    {
        //CRUD
        Task<IEnumerable<Admin>> GetAdmins();
        Task<Admin> GetAdmin(int id);
        Task CreateAdmin(Admin newAdmin);
        
    }
}
