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
            services.AddAutoMapper(config => config.AddProfile<ProductProfile>());
            services.AddScoped<IProductService, ProductService>();
            services.AddAutoMapper(config => config.AddProfile<CategoryProfile>());
            services.AddScoped<ICategoryService, CategoryService>();
            return services;
        }
    }
}
