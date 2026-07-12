using InShop.WebAPI.Extensions;
using InShop.WebAPI.Services;
using Microsoft.AspNetCore.Http.Features;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Extensions;
using InShopBLLayer.Services.Search;
using InShopDbModels.Extensions;
using StackExchange.Redis;

namespace InShop.WebAPI
{
    public partial class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.ValidateRequiredConfiguration(builder.Environment);

            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString) && builder.Environment.IsDevelopment())
            {
                redisConnectionString = "localhost:6379";
            }

            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString!));

            builder.Services.AddHttpClient();

            builder.Services.AddHttpClient<IEmbeddingService, HttpEmbeddingService>(client =>
            {
                var embeddingBaseUrl = builder.Configuration["Embedding:BaseUrl"]
                    ?? "http://localhost:8000/";
                client.BaseAddress = new Uri(embeddingBaseUrl);
            });


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    var allowedOrigins = builder.Configuration
                        .GetSection("Cors:AllowedOrigins")
                        .Get<string[]>()
                        ?? new[] { "http://localhost:3000" };

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

            builder.Services.AddInShopRepositories(connectionString);
            builder.Services.AddInShopServices(builder.Configuration);
            builder.Services.AddAdminIdentityAndJwt(builder.Configuration);
            builder.Services.AddSingleton<PaymentProcessingService>();
            // Оплата: мок (PaymentsAPI) или ЮKassa — см. Payment:Provider в appsettings.
            builder.Services.AddPaymentServices(builder.Configuration);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddAdminSwaggerJwt();
            builder.Services.AddHealthChecks();

            // Base64-изображения товаров до 5 МБ
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 6 * 1024 * 1024;
            });

            var app = builder.Build();

            await app.Services.EnsureDatabaseCreatedForDockerAsync(app.Configuration, app.Environment);
            await app.Services.SeedAdminRoleAsync();

            // ���������� CORS
            app.UseCors("AllowSpecificOrigin");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseStaticFiles();

            var uploadsRoot = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(Path.Combine(uploadsRoot, "products"));
            Directory.CreateDirectory(Path.Combine(uploadsRoot, "categories"));

            app.UseMiddleware<InShop.WebAPI.Middleware.SessionCsrfMiddleware>();
            app.UseMiddleware<InShop.WebAPI.Middleware.SessionMiddleware>();

            app.MapHealthChecks("/health");
            app.MapControllers();

            app.Run();
        }
    }
}
