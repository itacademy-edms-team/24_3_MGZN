namespace InShop.WebAPI.Extensions;

public static class ConfigurationValidationExtensions
{
    public static void ValidateRequiredConfiguration(this IConfiguration configuration, IWebHostEnvironment environment)
    {
        RequireValue(configuration.GetConnectionString("DefaultConnection"), "ConnectionStrings:DefaultConnection");
        RequireValue(configuration["Jwt:Key"], "Jwt:Key");

        if ((configuration["Jwt:Key"]?.Length ?? 0) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
        }

        var paymentProvider = configuration["Payment:Provider"] ?? "Mock";
        if (paymentProvider.Equals("YooKassa", StringComparison.OrdinalIgnoreCase))
        {
            RequireValue(configuration["Payment:YooKassa:ShopId"], "Payment:YooKassa:ShopId");
            RequireValue(configuration["Payment:YooKassa:SecretKey"], "Payment:YooKassa:SecretKey");
            RequireValue(configuration["Payment:YooKassa:WebhookSecret"], "Payment:YooKassa:WebhookSecret");
        }

        if (!environment.IsDevelopment())
        {
            RequireValue(configuration.GetConnectionString("Redis"), "ConnectionStrings:Redis");
            RequireValue(configuration["Email:SmtpServer"], "Email:SmtpServer");
            RequireValue(configuration["Email:Username"], "Email:Username");
            RequireValue(configuration["Email:Password"], "Email:Password");
        }
    }

    private static void RequireValue(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required configuration value '{key}' is missing.");
        }
    }
}
