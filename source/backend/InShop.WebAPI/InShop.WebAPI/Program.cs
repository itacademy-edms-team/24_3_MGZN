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

            var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                ?? "localhost:6379";
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));

            builder.Services.AddHttpClient();

            builder.Services.AddHttpClient<IEmbeddingService, HttpEmbeddingService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:8000/"); // ���������, ��� ����� ��������� � ���, �� ������� ������� FastAPI
            });


            //CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    policy.WithOrigins("http://localhost:3000") // ��������� ������� ������ � ����� ������
                          .AllowAnyHeader()                    // ��������� ����� ���������
                          .AllowAnyMethod()                   // ��������� ����� HTTP-������
                          .AllowCredentials();
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddInShopRepositories(connectionString);
            builder.Services.AddInShopServices(builder.Configuration);
            builder.Services.AddSingleton<PaymentProcessingService>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // ���������� CORS
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

            app.UseCors("AllowFrontend");

            app.UseMiddleware<InShop.WebAPI.Middleware.SessionMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}
