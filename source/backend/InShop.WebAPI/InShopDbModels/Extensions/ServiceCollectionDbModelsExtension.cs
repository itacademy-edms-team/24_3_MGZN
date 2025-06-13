using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Extensions
{
    public static class ServiceCollectionDbModelsExtension
    {
        public static IServiceCollection AddInShopRepositories(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AppDbContext>(options =>
                   options.UseSqlServer(connectionString));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IShipCompanyRepository, ShipCompanyRepository>();
            services.AddScoped<IUserSessionRepository, UserSessionRepository>();
            return services;
        }
    }
}
