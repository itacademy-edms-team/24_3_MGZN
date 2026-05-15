using Contracts.Optoins;
using InShopBLLayer.Abstractions;
using InShopBLLayer.MappingProfiles;
using InShopBLLayer.Services;
using InShopBLLayer.Services.Ai.Providers;
using InShopBLLayer.Services.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Extensions
{
    public static class ServiceCollectionBLLayerExtension
    {
        public static IServiceCollection AddInShopServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Кэш
            services.AddMemoryCache();

            services.AddAutoMapper(config => config.AddProfile<ProductProfile>());
            services.AddScoped<IProductService, ProductService>();
            services.AddAutoMapper(config => config.AddProfile<CategoryProfile>());
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddAutoMapper(config => config.AddProfile<ShipCompanyProfile>());
            services.AddScoped<IShipCompanyService, ShipCompanyService>();
            services.AddScoped<IUserSessionService, UserSessionService>();
            services.AddAutoMapper(config => config.AddProfile<UserSessionProfile>());
            services.AddScoped<IOrderService, OrderService>();
            services.AddAutoMapper(config => config.AddProfile<OrderProfile>());
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IPaymentStatusService, PaymentStatusService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddAutoMapper(config => config.AddProfile<ReviewProfile>());

            // Добавляем регистрацию AI сервисов
            services.AddAiServices(configuration);

            // Регистрация сервиса кэширования отзывов
            services.AddScoped<IReviewCacheService, ReviewCacheService>();

            services.AddHostedService<VectorIndexingService>();

            // ЮKassa и MockPaymentService регистрируются в InShop.WebAPI (PaymentServiceExtensions),
            // т.к. HTTP-клиент и контроллеры живут в проекте WebAPI, а не в BLLayer.

            return services;
        }

        public static IServiceCollection AddAiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var aiSettings = configuration.GetSection("AiSettings").Get<AiProviderSettings>();

            // Если настроек нет или ключ пустой, используем заглушку
            if (aiSettings == null || string.IsNullOrEmpty(aiSettings.ApiKey))
            {
                services.AddScoped<IAiProvider, NoOpAiProvider>();
                services.AddScoped<IAiAnalysisService, AiAnalysisService>();
                return services;
            }

            services.AddSingleton(aiSettings);

            // Регистрируем HttpClient для AI провайдеров
            services.AddHttpClient("AiProvider", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
            });

            // Factory для выбора провайдера
            services.AddScoped<IAiProvider>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("AiProvider");
                var settings = sp.GetRequiredService<AiProviderSettings>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                return settings.ProviderType.ToLower() switch
                {
                    "yandex" => new YandexGptProvider(httpClient, settings, loggerFactory.CreateLogger<YandexGptProvider>()),
                    // Здесь можно добавить другие провайдеры в будущем
                    _ => throw new InvalidOperationException($"Unknown AI provider: {settings.ProviderType}")
                };
            });

            services.AddScoped<IAiAnalysisService, AiAnalysisService>();

            return services;
        }
    }
}
