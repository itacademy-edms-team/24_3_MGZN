using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Dtos;
using InShopDbModels.Models;

namespace InShopBLLayer.Abstractions
{
    public interface IUserSessionService
    {
        Task<int> CreateUserSession(UserSessionDto userSessionDto);
        Task<UserSession> GetSession(int sessionid);
    }
}
