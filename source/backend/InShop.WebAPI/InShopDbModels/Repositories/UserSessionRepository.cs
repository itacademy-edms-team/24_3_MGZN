using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Repositories
{
    internal class UserSessionRepository : IUserSessionRepository
    {
        private readonly AppDbContext _appDbContext;
        public UserSessionRepository(AppDbContext context)
        {
            _appDbContext = context;
        }
        public async Task CreateUserSession(UserSession userSession)
        {
            await _appDbContext.UserSessions.AddAsync(userSession);
            await _appDbContext.SaveChangesAsync();
        }
    }
}
