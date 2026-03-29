using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
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
        public async Task<int> CreateUserSession(UserSession userSession)
        {
            await _appDbContext.UserSessions.AddAsync(userSession);
            await _appDbContext.SaveChangesAsync();
            return userSession.SessionId;
        }
        public async Task<UserSession> GetSessionById(int sessionId)
        {
            return await _appDbContext.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }
        // Поиск по токену
        public async Task<UserSession?> GetSessionByTokenAsync(Guid sessionToken)
        {
            return await _appDbContext.UserSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
        }

        // Обновление сессии
        public async Task UpdateSessionAsync(UserSession userSession)
        {
            _appDbContext.UserSessions.Update(userSession);
            await _appDbContext.SaveChangesAsync();
        }

        // Очистка старых сессий
        public async Task<int> CleanupExpiredSessionsAsync(DateTime cutoffDate)
        {
            var expiredSessions = _appDbContext.UserSessions
                .Where(s => s.ExpiresAt < cutoffDate || !s.IsActive);

            _appDbContext.UserSessions.RemoveRange(expiredSessions);
            return await _appDbContext.SaveChangesAsync();
        }
    }
}
