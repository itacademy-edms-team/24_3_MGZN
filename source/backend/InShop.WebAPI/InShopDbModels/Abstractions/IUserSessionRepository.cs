using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IUserSessionRepository
    {
        Task<int> CreateUserSession(UserSession userSession);
        Task<UserSession> GetSessionById(int sessionId);
    }
}
