using InShopBLLayer.Abstractions;
using InShopBLLayer.MappingProfiles;
using InShopBLLayer.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Extensions
{
    public static class ServiceCollectionBLLayerExtension
    {
        public static IServiceCollection AddInShopServices(this IServiceCollection services)
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

            return services;
        }
    }
}
