namespace InShop.WebAPI.Middleware;

public class SessionCsrfMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly HashSet<string> _allowedOrigins;

    public SessionCsrfMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
        _allowedOrigins = (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" })
            .Select(o => o.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method)
            || context.Request.Headers.ContainsKey("X-Session-Token")
            || !context.Request.Cookies.ContainsKey("SessionToken"))
        {
            await _next(context);
            return;
        }

        var requestOrigin = GetOrigin(context.Request);
        if (requestOrigin is null && _environment.IsDevelopment())
        {
            await _next(context);
            return;
        }

        if (requestOrigin is null || !_allowedOrigins.Contains(requestOrigin.TrimEnd('/')))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid request origin." });
            return;
        }

        await _next(context);
    }

    private static string? GetOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return origin;
        }

        var referer = request.Headers.Referer.FirstOrDefault();
        return Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)
            ? $"{refererUri.Scheme}://{refererUri.Authority}"
            : null;
    }
}
