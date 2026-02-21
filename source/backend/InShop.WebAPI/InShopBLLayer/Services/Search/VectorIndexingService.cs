using InShopBLLayer.Abstractions;        // IEmbeddingService
using InShopDbModels.Abstractions;       // IProductRepository, ICategoryRepository
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InShopBLLayer.Services.Search
{
    public class VectorIndexingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionMultiplexer _redis;
        private readonly ILogger<VectorIndexingService> _logger;
        private static readonly TimeSpan _defaultInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _interval;

        // ❌ Убрали IEmbeddingService, IProductRepository, ICategoryRepository из конструктора
        public VectorIndexingService(
            IServiceScopeFactory scopeFactory, // ← Для создания scope
            ConnectionMultiplexer redis,
            ILogger<VectorIndexingService> logger)
        {
            _scopeFactory = scopeFactory;
            _redis = redis;
            _logger = logger;
            _interval = _defaultInterval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Служба векторной индексации запущена");

            // ✅ Исправлено: было while(stoppingToken.IsCancellationRequested)
            // Это означало "делай, пока токен отменён" - бесконечный цикл или не запуск
            while (!stoppingToken.IsCancellationRequested) // <-- Добавили НЕ (!)
            {
                try
                {
                    await IndexProductsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка во время индексации векторов.");
                }

                _logger.LogInformation("Ожидание {Interval} до следующего запуска...", _interval);
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Служба векторной индексации остановлена.");
        }

        private async Task IndexProductsAsync(CancellationToken cancellationToken)
        {
            // ✅ Создаём новый scope внутри метода
            using var scope = _scopeFactory.CreateScope();

            // ✅ Получаем нужные сервисы из нового scope
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
            var db = _redis.GetDatabase(); // Redis не зависит от scope

            var products = await productRepository.GetProducts();
            _logger.LogInformation("Найдено {Count} товаров для индексации.", products.Count());

            // Определяем ожидаемую размерность вектора (для ai-forever/sbert_large_nlu_ru это 1024)
            const int ExpectedDimension = 768;

            foreach (var product in products)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var text = $"{product.ProductName} {product.ProductDescription?.Trim() ?? ""}".Trim();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        _logger.LogWarning("Товар ID {ProductId} имеет пустое имя и описание, пропускаем.", product.ProductId);
                        continue;
                    }

                    // ✅ Используем embeddingService из текущего scope
                    // Убедитесь, что embeddingService (через FastAPI) использует ai-forever/sbert_large_nlu_ru
                    var vector = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken);

                    // Проверяем размерность вектора (ожидаем 1024 для sbert_large_nlu_ru)
                    if (vector.Length != ExpectedDimension)
                    {
                        _logger.LogError("Вектор для товара ID {ProductId} имеет неверную размерность: {ActualLength}. Ожидается {ExpectedLength} для модели ai-forever/sbert_large_nlu_ru.", product.ProductId, vector.Length, ExpectedDimension);
                        continue; // Пропускаем этот товар
                    }

                    var vectorBytes = new byte[vector.Length * sizeof(float)];
                    Buffer.BlockCopy(vector, 0, vectorBytes, 0, vectorBytes.Length);

                    // ✅ Используем categoryRepository из текущего scope
                    var categoryName = await categoryRepository.GetCategoryNameById(product.ProductCategoryId);

                    var availability = product.ProductAvailability == true ? "InStock" : "OutOfStock";

                    var hash = new HashEntry[]
                    {
                        new HashEntry("name", product.ProductName ?? ""),
                        new HashEntry("description", product.ProductDescription ?? ""),
                        new HashEntry("embedding", vectorBytes), // Вектор длиной 1024 * 4 байта = 4096 байт
                        new HashEntry("category", categoryName),
                        new HashEntry("price", ((double)product.ProductPrice).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        new HashEntry("stock", product.ProductStockQuantity.ToString()),
                        new HashEntry("availability", availability),
                        new HashEntry("image_url", product.ImageUrl ?? "")
                    };

                    await db.HashSetAsync($"product:{product.ProductId}", hash);
                    _logger.LogDebug("Товар ID {ProductId} сохранён в Redis.", product.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при индексации товара ID {ProductId}", product.ProductId);
                }
            }

            await CreateOrUpdateIndexAsync();
        }

        private async Task CreateOrUpdateIndexAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            try
            {
                try { await server.ExecuteAsync("FT.DROPINDEX", "idx:products"); } catch { /* Игнорируем ошибку, если индекс не существует */ }

                const int IndexDimension = 768; // ✅ Установлено на 1024 для ai-forever/sbert_large_nlu_ru

                // ✅ Обновлённая команда CREATE INDEX с DIM 1024 для ai-forever/sbert_large_nlu_ru
                await server.ExecuteAsync(
                    "FT.CREATE", "idx:products",
                    "ON", "HASH",
                    "PREFIX", "1", "product:",
                    "SCHEMA",
                    "name", "TEXT",
                    "description", "TEXT",
                    "category", "TAG",
                    "price", "NUMERIC",
                    "stock", "NUMERIC",
                    "availability", "TAG",
                    "image_url", "TEXT",
                    "embedding", "VECTOR", "FLAT", "6", // "6" - это количество аргументов после "FLAT"
                    "TYPE", "FLOAT32",                 // Тип данных вектора
                    "DIM", IndexDimension.ToString(),   // ✅ Изменено: размерность 1024 для sbert_large_nlu_ru
                    "DISTANCE_METRIC", "COSINE"        // Метрика расстояния
                );

                _logger.LogInformation("Векторный индекс 'idx:products' создан с размерностью {Dimension} для модели ai-forever/sbert_large_nlu_ru.", IndexDimension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании векторного индекса.");
                throw;
            }
        }
    }
}