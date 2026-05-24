using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InShopBLLayer.Services.Search
{
    /// <summary>
    /// Полная перестройка Redis Search индекса. Общая логика для фоновой службы и админки.
    /// </summary>
    public class VectorSearchIndexRebuildService : IVectorSearchIndexRebuildService
    {
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

        public async Task RebuildFullIndexAsync(CancellationToken cancellationToken = default)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();

            var indicesResult = await server.ExecuteAsync("FT._LIST");
            var indexList = new List<string>();

            if (indicesResult.Type == ResultType.MultiBulk)
            {
                var indicesArray = (RedisResult[])indicesResult;
                indexList = indicesArray.Select(x => x.ToString()).ToList();
            }

            if (indexList.Contains("idx:products"))
            {
                await server.ExecuteAsync("FT.DROPINDEX", "idx:products", "DD");
                _logger.LogInformation("Старый индекс 'idx:products' удалён.");
            }

            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

            var products = await productRepository.GetProducts();
            _logger.LogInformation("Индексация {Count} товаров в Redis.", products.Count());

            const int expectedDimension = 768;
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

                    var baseText = $"{categoryName}; {product.ProductName}; {product.ProductDescription?.Trim() ?? ""}";
                    var specPhrases = new List<string>();
                    var importantKeywords = new HashSet<string>();

                    foreach (var spec in specs)
                    {
                        string valueStr;
                        if (spec.ValueNumber.HasValue)
                            valueStr = spec.ValueNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        else if (!string.IsNullOrEmpty(spec.ValueText))
                            valueStr = spec.ValueText;
                        else
                            continue;

                        specPhrases.Add($"{spec.DisplayName}: {valueStr}");
                        importantKeywords.Add(valueStr);
                    }

                    var fullTextParts = new List<string> { baseText };
                    if (specPhrases.Any())
                    {
                        fullTextParts.Add("Характеристики: " + string.Join("; ", specPhrases));
                        if (importantKeywords.Any())
                        {
                            var keywordsString = string.Join(", ", importantKeywords);
                            fullTextParts[0] = $"{baseText}, Ключевые особенности: {keywordsString}";
                        }
                    }

                    var fullText = string.Join(". ", fullTextParts);
                    if (string.IsNullOrWhiteSpace(fullText))
                    {
                        continue;
                    }

                    var vector = await embeddingService.GenerateEmbeddingAsync(fullText, cancellationToken);
                    if (vector.Length != expectedDimension)
                    {
                        _logger.LogError("Неверная размерность вектора для товара {ProductId}.", product.ProductId);
                        continue;
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
                        string valueToStore;
                        if (spec.ValueNumber.HasValue)
                            valueToStore = spec.ValueNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        else if (!string.IsNullOrEmpty(spec.ValueText))
                            valueToStore = spec.ValueText;
                        else
                            continue;

                        hashEntries.Add(new HashEntry(spec.Name, valueToStore));
                    }

                    await db.HashSetAsync($"product:{product.ProductId}", hashEntries.ToArray());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка индексации товара {ProductId}", product.ProductId);
                }
            }

            await CreateIndexAsync(cancellationToken);
            _logger.LogInformation("Полная переиндексация Redis завершена.");
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
                "idx:products", "ON", "HASH", "PREFIX", "1", "product:", "SCHEMA",
                "name", "TEXT", "WEIGHT", "5.0",
                "description", "TEXT", "WEIGHT", "1.0",
                "category", "TAG", "SEPARATOR", ";",
                "price", "NUMERIC", "stock", "NUMERIC", "availability", "TAG", "image_url", "TEXT",
                "embedding", "VECTOR", "FLAT", "6", "TYPE", "FLOAT32", "DIM", "768", "DISTANCE_METRIC", "COSINE"
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
    }
}
