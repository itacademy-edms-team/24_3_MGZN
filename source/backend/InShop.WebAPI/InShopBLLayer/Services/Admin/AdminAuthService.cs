using Contracts.Admin.Dto;
using Contracts.Admin.Options;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InShopBLLayer.Services.Admin
{
    /// <summary>
    /// Аутентификация администраторов через ASP.NET Identity и JWT (без cookie-сессии покупателя).
    /// </summary>
    public class AdminAuthService : IAdminAuthService
    {
        public const string AdminRoleName = "Admin";

        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AdminAuthService> _logger;

        public AdminAuthService(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AdminAuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Регистрация единственного первого админа. Email = корпоративный логин (UserName).
        /// </summary>
        public async Task RegisterFirstAdminAsync(AdminRegisterDto dto, CancellationToken ct = default)
        {
            if (await _userManager.Users.AnyAsync(ct))
            {
                throw new InvalidOperationException("Регистрация закрыта: администратор уже существует.");
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errors);
            }

            await _userManager.AddToRoleAsync(user, AdminRoleName);
            _logger.LogInformation("Создан первый администратор {Email}", email);
        }

        public async Task<AdminAuthResponseDto> LoginAsync(AdminLoginDto dto, CancellationToken ct = default)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                throw new UnauthorizedAccessException("Неверный email или пароль.");
            }

            // SignInManager проверяет пароль и блокировку учётной записи (без cookie).
            var signIn = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!signIn.Succeeded)
            {
                throw new UnauthorizedAccessException("Неверный email или пароль.");
            }

            if (!await _userManager.IsInRoleAsync(user, AdminRoleName))
            {
                throw new UnauthorizedAccessException("Учётная запись не имеет роли администратора.");
            }

            return BuildTokenResponse(user);
        }

        public AdminMeDto GetMe(ClaimsPrincipal user)
        {
            var email = user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? user.Identity?.Name
                ?? string.Empty;

            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return new AdminMeDto
            {
                Email = email,
                Roles = roles
            };
        }

        private AdminAuthResponseDto BuildTokenResponse(IdentityUser user)
        {
            var expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? user.UserName ?? ""),
                new(ClaimTypes.Email, user.Email ?? user.UserName ?? ""),
                new(ClaimTypes.Name, user.Email ?? user.UserName ?? ""),
                new(ClaimTypes.Role, AdminRoleName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new AdminAuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Email = user.Email ?? user.UserName ?? "",
                ExpiresAtUtc = expires
            };
        }
    }
}
