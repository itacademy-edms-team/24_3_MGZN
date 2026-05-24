using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InShopBLLayer.Services.Search
{
    /// <summary>
    /// Индексация каталога в Redis Search: точечная (админка) и полная перестройка (фон).
    /// </summary>
    public class VectorSearchIndexRebuildService : IVectorSearchIndexRebuildService
    {
        private const int ExpectedEmbeddingDimension = 768;
        private const string IndexName = "idx:products";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<VectorSearchIndexRebuildService> _logger;

        public VectorSearchIndexRebuildService(
            IServiceScopeFactory scopeFactory,
            IConnectionMultiplexer redis,
            ILogger<VectorSearchIndexRebuildService> logger)
        {
            _scopeFactory = scopeFactory;
            _redis = redis;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task IndexProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            await EnsureIndexExistsAsync(cancellationToken);

            using var scope = _scopeFactory.CreateScope();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var product = await productRepository.GetProduct(productId);
            if (product is null)
            {
                _logger.LogWarning("Точечная индексация: товар {ProductId} не найден в БД", productId);
                return;
            }

            var categoryName = product.ProductCategory?.CategoryName
                ?? await categoryRepository.GetCategoryNameById(product.ProductCategoryId);

            var rawSpecs = await productRepository.GetProductSpecificationsAsync(productId);
            var specs = MapSpecs(productId, rawSpecs);

            var db = _redis.GetDatabase();
            var indexed = await UpsertProductHashAsync(
                product,
                categoryName,
                specs,
                embeddingService,
                db,
                cancellationToken);

            if (indexed)
            {
                _logger.LogInformation("Товар {ProductId} проиндексирован в Redis (точечно)", productId);
            }
        }

        /// <inheritdoc />
        public async Task RemoveProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var db = _redis.GetDatabase();
            var deleted = await db.KeyDeleteAsync($"product:{productId}");

            if (deleted)
            {
                _logger.LogInformation("Товар {ProductId} удалён из Redis-индекса", productId);
            }
            else
            {
                _logger.LogDebug("Ключ product:{ProductId} отсутствовал в Redis", productId);
            }
        }

        /// <inheritdoc />
        public async Task RebuildFullIndexAsync(CancellationToken cancellationToken = default)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();

            var indexList = await GetIndexNamesAsync(server);
            if (indexList.Contains(IndexName))
            {
                await server.ExecuteAsync("FT.DROPINDEX", IndexName, "DD");
                _logger.LogInformation("Старый индекс '{IndexName}' удалён.", IndexName);
            }

            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

            var products = await productRepository.GetProducts();
            _logger.LogInformation("Индексация {Count} товаров в Redis.", products.Count());

            var allSpecs = await productRepository.GetAllProductSpecificationsRawAsync(cancellationToken);
            var specsByProductId = allSpecs.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var product in products)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var categoryName = await categoryRepository.GetCategoryNameById(product.ProductCategoryId);
                    var specs = specsByProductId.TryGetValue(product.ProductId, out var list)
                        ? list
                        : new List<(int ProductId, string Name, string DisplayName, string? ValueText, decimal? ValueNumber)>();

                    await UpsertProductHashAsync(product, categoryName, specs, embeddingService, db, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка индексации товара {ProductId}", product.ProductId);
                }
            }

            await CreateIndexAsync(cancellationToken);
            _logger.LogInformation("Полная переиндексация Redis завершена.");
        }

        /// <summary>
        /// Создаёт idx:products, если его ещё нет (первый save после деплоя или после сбоя Redis).
        /// </summary>
        private async Task EnsureIndexExistsAsync(CancellationToken cancellationToken)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var indexList = await GetIndexNamesAsync(server);

            if (!indexList.Contains(IndexName))
            {
                _logger.LogInformation("Индекс '{IndexName}' не найден — создаём перед точечной индексацией", IndexName);
                await CreateIndexAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Общая логика записи hash product:{id}. Используется и full rebuild, и IndexProductAsync.
        /// </summary>
        private async Task<bool> UpsertProductHashAsync(
            Product product,
            string categoryName,
            IReadOnlyList<(int ProductId, string Name, string DisplayName, string? ValueText, decimal? ValueNumber)> specs,
            IEmbeddingService embeddingService,
            IDatabase db,
            CancellationToken cancellationToken)
        {
            var fullText = BuildEmbeddingText(product, categoryName, specs);
            if (string.IsNullOrWhiteSpace(fullText))
            {
                return false;
            }

            var vector = await embeddingService.GenerateEmbeddingAsync(fullText, cancellationToken);
            if (vector.Length != ExpectedEmbeddingDimension)
            {
                _logger.LogError("Неверная размерность вектора для товара {ProductId}.", product.ProductId);
                return false;
            }

            var vectorBytes = new byte[vector.Length * sizeof(float)];
            Buffer.BlockCopy(vector, 0, vectorBytes, 0, vectorBytes.Length);

            var hashEntries = new List<HashEntry>
            {
                new("name", product.ProductName ?? ""),
                new("description", product.ProductDescription ?? ""),
                new("embedding", vectorBytes),
                new("category", categoryName),
                new("price", ((double)product.ProductPrice).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("stock", product.ProductStockQuantity.ToString()),
                new("availability", product.ProductAvailability ? "InStock" : "OutOfStock"),
                new("image_url", product.ImageUrl ?? "")
            };

            foreach (var spec in specs)
            {
                if (!TryGetSpecValue(spec.ValueText, spec.ValueNumber, out var valueToStore))
                {
                    continue;
                }

                hashEntries.Add(new HashEntry(spec.Name, valueToStore));
            }

            await db.HashSetAsync($"product:{product.ProductId}", hashEntries.ToArray());
            return true;
        }

        private static string BuildEmbeddingText(
            Product product,
            string categoryName,
            IReadOnlyList<(int ProductId, string Name, string DisplayName, string? ValueText, decimal? ValueNumber)> specs)
        {
            var baseText = $"{categoryName}; {product.ProductName}; {product.ProductDescription?.Trim() ?? ""}";
            var specPhrases = new List<string>();
            var importantKeywords = new HashSet<string>();

            foreach (var spec in specs)
            {
                if (!TryGetSpecValue(spec.ValueText, spec.ValueNumber, out var valueStr))
                {
                    continue;
                }

                specPhrases.Add($"{spec.DisplayName}: {valueStr}");
                importantKeywords.Add(valueStr);
            }

            var fullTextParts = new List<string> { baseText };
            if (specPhrases.Count > 0)
            {
                fullTextParts.Add("Характеристики: " + string.Join("; ", specPhrases));
                if (importantKeywords.Count > 0)
                {
                    var keywordsString = string.Join(", ", importantKeywords);
                    fullTextParts[0] = $"{baseText}, Ключевые особенности: {keywordsString}";
                }
            }

            return string.Join(". ", fullTextParts);
        }

        private static bool TryGetSpecValue(string? valueText, decimal? valueNumber, out string valueToStore)
        {
            if (valueNumber.HasValue)
            {
                valueToStore = valueNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }

            if (!string.IsNullOrEmpty(valueText))
            {
                valueToStore = valueText;
                return true;
            }

            valueToStore = string.Empty;
            return false;
        }

        private static List<(int ProductId, string Name, string DisplayName, string? ValueText, decimal? ValueNumber)> MapSpecs(
            int productId,
            List<(int SpecId, string Name, string DisplayName, string DataType, string? TextValue, decimal? NumberValue)>? rawSpecs)
        {
            if (rawSpecs is null || rawSpecs.Count == 0)
            {
                return new List<(int, string, string, string?, decimal?)>();
            }

            return rawSpecs
                .Select(s => (productId, s.Name, s.DisplayName, s.TextValue, s.NumberValue))
                .ToList();
        }

        private async Task CreateIndexAsync(CancellationToken cancellationToken)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            using var scope = _scopeFactory.CreateScope();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var allSpecNames = await productRepository.GetAllProductSpecificationsRawAsync(cancellationToken);
            var uniqueSpecNames = allSpecNames.Select(s => s.Name).Distinct().ToList();

            var knownSpecTypes = new Dictionary<string, string>
            {
                { "ram_gb", "NUMERIC" },
                { "storage_gb", "NUMERIC" },
                { "refresh_rate_hz", "NUMERIC" },
                { "screen_size_inch", "NUMERIC" },
                { "weight_g", "NUMERIC" },
                { "weight_kg", "NUMERIC" },
            };

            var args = new List<object>
            {
                IndexName, "ON", "HASH", "PREFIX", "1", "product:", "SCHEMA",
                "name", "TEXT", "WEIGHT", "5.0",
                "description", "TEXT", "WEIGHT", "1.0",
                "category", "TAG", "SEPARATOR", ";",
                "price", "NUMERIC", "stock", "NUMERIC", "availability", "TAG", "image_url", "TEXT",
                "embedding", "VECTOR", "FLAT", "6", "TYPE", "FLOAT32", "DIM", ExpectedEmbeddingDimension, "DISTANCE_METRIC", "COSINE"
            };

            foreach (var specName in uniqueSpecNames)
            {
                var fieldType = knownSpecTypes.GetValueOrDefault(specName, "TAG");
                args.Add(specName);
                args.Add(fieldType);
                if (fieldType == "TAG")
                {
                    args.Add("SEPARATOR");
                    args.Add(";");
                }
            }

            await server.ExecuteAsync("FT.CREATE", args.ToArray());
        }

        private static async Task<List<string>> GetIndexNamesAsync(IServer server)
        {
            var indicesResult = await server.ExecuteAsync("FT._LIST");
            if (indicesResult.Type != ResultType.MultiBulk)
            {
                return new List<string>();
            }

            var indicesArray = (RedisResult[])indicesResult;
            return indicesArray.Select(x => x.ToString()).ToList()!;
        }
    }
}
