using Microsoft.AspNetCore.Mvc;
using InShopBLLayer.Abstractions;
using Contracts.Dtos;

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
            await _userSessionService.CreateUserSession(userSessionDto);
            return Ok("Сессия создана");
        }
    }
}
