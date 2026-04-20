using InShopDbModels.Abstractions;

namespace InShop.WebAPI.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserSessionRepository sessionRepository)
        {
            // Пытаемся получить токен из заголовка или куки
            string? tokenString = context.Request.Headers["X-Session-Token"].FirstOrDefault()
                                  ?? context.Request.Cookies["SessionToken"];

            if (!string.IsNullOrEmpty(tokenString) && Guid.TryParse(tokenString, out Guid sessionToken))
            {
                // Ищем сессию в БД
                var session = await sessionRepository.GetSessionByTokenAsync(sessionToken);

                if (session != null && session.IsActive && session.ExpiresAt > DateTime.UtcNow)
                {
                    // Кладем SessionId в контекст запроса, чтобы использовать в контроллерах
                    context.Items["SessionId"] = session.SessionId;
                }
            }

            await _next(context);
        }
    }
}
