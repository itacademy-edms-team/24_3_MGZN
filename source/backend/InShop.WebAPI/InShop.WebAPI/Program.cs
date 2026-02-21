using InShop.WebAPI.Services;
using InShopBLLayer.Abstractions;
using InShopBLLayer.Extensions;
using InShopBLLayer.Services.Search;
using InShopDbModels.Extensions;
using StackExchange.Redis;

namespace InShop.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton(ConnectionMultiplexer.Connect("localhost:6379"));

            builder.Services.AddHttpClient();

            builder.Services.AddHttpClient<IEmbeddingService, HttpEmbeddingService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:8000/"); // Убедитесь, что адрес совпадает с тем, на котором запущен FastAPI
                                                                        // Можно добавить таймауты, заголовки и т.д.
            });


            //CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    policy.WithOrigins("http://localhost:3000") // Разрешаем запросы только с этого домена
                          .AllowAnyHeader()                    // Разрешаем любые заголовки
                          .AllowAnyMethod();                   // Разрешаем любые HTTP-методы
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddInShopRepositories(connectionString);
            builder.Services.AddInShopServices();
            builder.Services.AddSingleton<PaymentProcessingService>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Используем CORS
            app.UseCors("AllowSpecificOrigin");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
