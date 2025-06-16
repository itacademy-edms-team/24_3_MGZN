using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserSessionController : ControllerBase
    {
        private readonly IUserSessionService _userSessionService;
        public UserSessionController(IUserSessionService userSessionService)
        {
            _userSessionService = userSessionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] UserSessionDto userSessionDto)
        {
            var sessionId = await _userSessionService.CreateUserSession(userSessionDto);
            return Ok(new SessionCreationResult
            {
                SessionId = sessionId,
                Message = "Сессия успешно создана"
            });
        }
    }
}
