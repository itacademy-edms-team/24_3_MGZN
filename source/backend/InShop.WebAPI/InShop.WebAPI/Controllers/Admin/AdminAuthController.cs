using Contracts.Admin.Dto;
using InShop.WebAPI.Extensions;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InShop.WebAPI.Controllers.Admin
{
    /// <summary>
    /// Аутентификация администратора (JWT). Не использует SessionToken покупателя.
    /// </summary>
    [ApiController]
    [Route("api/Admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IAdminAuthService _adminAuthService;
        private readonly ILogger<AdminAuthController> _logger;

        public AdminAuthController(IAdminAuthService adminAuthService, ILogger<AdminAuthController> logger)
        {
            _adminAuthService = adminAuthService;
            _logger = logger;
        }

        /// <summary>
        /// Временная регистрация первого админа. После появления пользователя — 409 Conflict.
        /// Удалите эндпоинт после создания учётной записи в production.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AdminAuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] AdminRegisterDto dto, CancellationToken ct)
        {
            try
            {
                await _adminAuthService.RegisterFirstAdminAsync(dto, ct);
                var login = await _adminAuthService.LoginAsync(new AdminLoginDto
                {
                    Email = dto.Email,
                    Password = dto.Password
                }, ct);
                return Created(string.Empty, login);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("уже существует"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AdminAuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _adminAuthService.LoginAsync(dto, ct);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize(Policy = AdminIdentityExtensions.AdminOnlyPolicy)]
        [ProducesResponseType(typeof(AdminMeDto), StatusCodes.Status200OK)]
        public IActionResult Me()
        {
            return Ok(_adminAuthService.GetMe(User));
        }
    }
}
