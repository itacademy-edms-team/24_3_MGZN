﻿using InShopBLLayer.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InShopDbModels.Abstractions;
using InShopDbModels.Repositories;
using AutoMapper;
using InShopDbModels.Models;
using Contracts.Dtos;

namespace InShopBLLayer.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserSessionRepository _repository;
        private readonly IMapper _mapper;
        public UserSessionService(IUserSessionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<int> CreateUserSession(UserSessionDto userSessionDto)
        {
            var session = _mapper.Map<UserSession>(userSessionDto);
            return await _repository.CreateUserSession(session);
        }
        public async Task<UserSession> GetSession(int sessionId)
        {
            return await _repository.GetSessionById(sessionId);
        } 
    }
}
