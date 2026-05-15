using InShop.WebAPI.Services.Payment;
using InShop.WebAPI.Services.Payment.Clients;

namespace InShop.WebAPI.Extensions
{
    /// <summary>
    /// Регистрация сервисов оплаты (мок и ЮKassa).
    /// Вызывается из Program.cs после AddInShopServices.
    /// </summary>
    public static class PaymentServiceExtensions
    {
        public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Мок всегда доступен — переключение провайдера в PaymentController.
            services.AddScoped<MockPaymentService>();

            // Регистрация ЮKassa только если в конфиге выбран этот провайдер.
            // Условная регистрация не создаёт лишних HttpClient и не требует ключей при работе с моком.
            if (string.Equals(configuration["Payment:Provider"], "YooKassa", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHttpClient<YooKassaClient>();
                services.AddScoped<YooKassaPaymentService>();
            }

            return services;
        }
    }
}
