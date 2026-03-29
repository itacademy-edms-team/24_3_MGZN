using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using InShopDbModels.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserSessionService> _logger;
        public UserSessionService(IUserSessionRepository repository, IMapper mapper, ILogger<UserSessionService> logger)
        {
            _userSessionRepository = repository;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<(SessionCreationResult Result, Guid SessionToken)> CreateUserSessionAsync(UserSessionDto dto)
        {
            var session = _mapper.Map<UserSession>(dto);

            session.SessionToken = Guid.NewGuid();
            session.CreatedAt = DateTime.UtcNow;
            session.ExpiresAt = DateTime.UtcNow.AddDays(30);
            session.IsActive = true;

            var sessionId = await _userSessionRepository.CreateUserSession(session);
            session.SessionId = sessionId;


            _logger.LogInformation(
            "Session created: SessionId={SessionId}, IP={IP}",
            sessionId,
            dto.UserIpaddress);

            var result = _mapper.Map<SessionCreationResult>(session);
            result.Message = "Сессия успешно создана";

            return (Result: result, SessionToken: session.SessionToken);
        }
        
        public async Task<SessionValidationResult> ValidateSessionAsync(Guid sessionToken)
        {
            var session = await _userSessionRepository.GetSessionByTokenAsync(sessionToken);

            if (session == null)
            {
                _logger.LogWarning("Session not found for token: {Token}", sessionToken);
                return new SessionValidationResult
                {
                    IsValid = false,
                    Message = "Session not found"
                };
            }

            if (!session.IsActive)
            {
                _logger.LogWarning("Session {SessionId} is inactive", session.SessionId);
                return new SessionValidationResult
                {
                    IsValid = false,
                    Message = "Session is inactive"
                };
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Session {SessionId} expired", session.SessionId);
                return new SessionValidationResult
                {
                    IsValid = false,
                    Message = "Session expired"
                };
            }
            
            session.ExpiresAt = DateTime.UtcNow.AddDays(30);
            await _userSessionRepository.UpdateSessionAsync(session);

            _logger.LogDebug("Session {SessionId} validated and extended", session.SessionId);

            var result = _mapper.Map<SessionValidationResult>(session);
            result.Message = "Session valid";

            return result;
        }

        public async Task InvalidateSessionAsync(Guid sessionToken)
        {
            var session = await _userSessionRepository.GetSessionByTokenAsync(sessionToken);

            if (session != null)
            {
                session.IsActive = false;
                await _userSessionRepository.UpdateSessionAsync(session);

                _logger.LogInformation("Session {SessionId} invalidated", session.SessionId);
            }
        }

        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            var deletedCount = await _userSessionRepository.CleanupExpiredSessionsAsync(cutoffDate);

            _logger.LogInformation("Cleaned up {Count} expired sessions", deletedCount);

            return deletedCount;
        }
    }
}
