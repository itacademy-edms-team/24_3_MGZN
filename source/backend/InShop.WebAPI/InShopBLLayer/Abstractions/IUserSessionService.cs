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
        Task<(SessionCreationResult Result, Guid SessionToken)> CreateUserSessionAsync(UserSessionDto userSessionDto);

        Task<SessionValidationResult> ValidateSessionAsync(Guid sessionToken);

        Task InvalidateSessionAsync(Guid sessionToken);

        Task<int> CleanupExpiredSessionsAsync();
    }
}
