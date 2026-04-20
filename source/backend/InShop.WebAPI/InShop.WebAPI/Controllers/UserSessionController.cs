using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InShop.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserSessionController : ControllerBase
{
    private readonly IUserSessionService _userSessionService;
    private readonly IOrderService _orderService;
    private readonly ILogger<UserSessionController> _logger;

    public UserSessionController(
        IUserSessionService userSessionService,
        IOrderService orderService,
        ILogger<UserSessionController> logger)
    {
        _userSessionService = userSessionService;
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] UserSessionDto? userSessionDto = null)
    {
        _logger.LogInformation("CreateSession: Request received from {RemoteIp}",
            HttpContext.Connection.RemoteIpAddress);

        try
        {
            // 1. Подготовка данных
            userSessionDto ??= new UserSessionDto();
            userSessionDto.UserIpaddress ??= HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // ✅ ИСПРАВЛЕНО: "User-Agent" с дефисом
            var userAgentHeader = Request.Headers["User-Agent"].FirstOrDefault();
            userSessionDto.UserAgent ??= userAgentHeader ?? "unknown";

            _logger.LogDebug("Creating session for IP={IP}, UserAgent={UA}",
                userSessionDto.UserIpaddress, userSessionDto.UserAgent);

            // 2. Вызов сервиса
            var (result, sessionToken) = await _userSessionService.CreateUserSessionAsync(userSessionDto);

            _logger.LogDebug("Session created: SessionId={SessionId}, Token={Token}",
                result.SessionId, sessionToken);

            // 3. Создание заказа-черновика
            var orderDto = new OrderDto
            {
                SessionId = result.SessionId,
                OrderStatus = "Draft",
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                ShipMethod = "draft",
                PayStatus = "Unpayed",
                CustomerFullname = "draft",
                PayMethod = "draft",
                CustomerEmail = "draft",
                CustomerPhoneNumber = "draft",
            };
            var orderId = await _orderService.CreateNewOrder(orderDto);
            result.OrderId = orderId;

            _logger.LogDebug("Draft order created: OrderId={OrderId}", orderId);

            // 4. Установка куки
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = result.ExpiresAt != default ? result.ExpiresAt : DateTime.UtcNow.AddDays(30),
                Path = "/",
                IsEssential = true
            };

            Response.Cookies.Append("SessionToken", sessionToken.ToString(), cookieOptions);

            _logger.LogInformation("Session created: IP={IP}, OrderId={OrderId}, SessionId={SessionId}",
                userSessionDto.UserIpaddress, orderId, result.SessionId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSession: Unhandled exception");

#if DEBUG
            return StatusCode(500, new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
#else
    return StatusCode(500, new { error = "Internal server error" });
#endif
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionToken = Request.Cookies["SessionToken"];

            if (!string.IsNullOrEmpty(sessionToken) &&
                Guid.TryParse(sessionToken, out var tokenGuid))
            {
                await _userSessionService.InvalidateSessionAsync(tokenGuid);
            }

            Response.Cookies.Delete("SessionToken");

            _logger.LogInformation("Session logged out");

            return Ok(new { Message = "Сессия завершена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout: Error");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateSession()
    {
        try
        {
            var sessionToken = Request.Cookies["SessionToken"];

            // ✅ БЕЗОПАСНОЕ логирование (без Substring)
            _logger.LogInformation("=== ValidateSession DEBUG ===");
            _logger.LogInformation("Cookie header present: {HasCookie}",
                Request.Headers.ContainsKey("Cookie"));

            var rawCookieHeader = Request.Headers["Cookie"].FirstOrDefault();
            _logger.LogInformation("Raw Cookie header: {Header}",
                string.IsNullOrEmpty(rawCookieHeader) ? "N/A" :
                rawCookieHeader.Length > 100 ? rawCookieHeader.Substring(0, 100) + "..." : rawCookieHeader);

            _logger.LogInformation("SessionToken present: {Present}, Length: {Length}",
                !string.IsNullOrEmpty(sessionToken),
                sessionToken?.Length ?? 0);

            if (string.IsNullOrEmpty(sessionToken))
            {
                _logger.LogWarning("ValidateSession: SessionToken cookie is NULL or EMPTY");
                return Unauthorized(new { isValid = false, message = "No session token" });
            }

            if (!Guid.TryParse(sessionToken, out var tokenGuid))
            {
                _logger.LogWarning("ValidateSession: Invalid GUID format. Token length: {Length}", sessionToken.Length);
                return Unauthorized(new { isValid = false, message = "Invalid token format" });
            }

            _logger.LogDebug("Token parsed successfully: {Token}", sessionToken);

            var result = await _userSessionService.ValidateSessionAsync(tokenGuid);

            _logger.LogInformation("ValidateSession: Service result IsValid={IsValid}", result.IsValid);

            if (!result.IsValid)
            {
                return Unauthorized(result);
            }

            // Keep cookie lifetime in sync with sliding expiration from DB.
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = result.ExpiresAt != default ? result.ExpiresAt : DateTime.UtcNow.AddDays(30),
                Path = "/",
                IsEssential = true
            };

            Response.Cookies.Append("SessionToken", tokenGuid.ToString(), cookieOptions);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateSession: Error");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}