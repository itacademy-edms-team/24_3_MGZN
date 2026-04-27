using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InShopBLLayer.Services.Search
{
    public class VectorIndexingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<VectorIndexingService> _logger;
        private static readonly TimeSpan _defaultInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _interval;

        public VectorIndexingService(
            IServiceScopeFactory scopeFactory,
            IConnectionMultiplexer redis,
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

            while (!stoppingToken.IsCancellationRequested)
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
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();

            // --- ШАГ 1: УДАЛЯЕМ СТАРЫЙ ИНДЕКС И ВСЕ СВЯЗАННЫЕ HASH-КЛЮЧИ (ЕСЛИ СУЩЕСТВУЕТ) ---
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
                _logger.LogInformation("Старый индекс 'idx:products' и все связанные Hash-ключи удалены.");
            }
            else
            {
                _logger.LogInformation("Индекс 'idx:products' не найден, удаление пропущено.");
            }
            // --- КОНЕЦ ШАГА 1 ---

            // --- ШАГ 2: ЗАГРУЖАЕМ ДАННЫЕ И СОЗДАЕМ НОВЫЕ HASH-КЛЮЧИ ---
            using var scope = _scopeFactory.CreateScope();

            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

            var products = await productRepository.GetProducts();

            _logger.LogInformation("Найдено {Count} товаров для индексации.", products.Count());

            const int ExpectedDimension = 768;

            // --- ОПТИМИЗАЦИЯ 1: Загружаем ВСЕ характеристики ОДИН РАЗ ---
            // Теперь метод возвращает DisplayName
            var allSpecs = await productRepository.GetAllProductSpecificationsRawAsync(cancellationToken);

            // Группируем характеристики по ID товара
            var specsByProductId = allSpecs.GroupBy(s => s.ProductId)
                                           .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var product in products)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // 1. Получаем имя категории
                    var categoryName = await categoryRepository.GetCategoryNameById(product.ProductCategoryId);

                    // 2. Получаем характеристики ТОЛЬКО для этого товара из памяти
                    // Обратите внимание: теперь в списке есть свойство DisplayName
                    var specs = specsByProductId.TryGetValue(product.ProductId, out var list)
                        ? list
                        : new List<(int ProductId, string Name, string DisplayName, string? ValueText, decimal? ValueNumber)>();

                    // 3. Формируем текст для вектора (ОБНОВЛЕННАЯ ЛОГИКА)

                    // Базовый текст
                    var baseText = $"{categoryName}; {product.ProductName}; {product.ProductDescription?.Trim() ?? ""}";

                    var specPhrases = new List<string>();
                    var importantKeywords = new HashSet<string>(); // Для дублирования ключевых слов

                    if (specs.Any())
                    {
                        foreach (var spec in specs)
                        {
                            string valueStr;
                            if (spec.ValueNumber.HasValue)
                                valueStr = spec.ValueNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            else if (!string.IsNullOrEmpty(spec.ValueText))
                                valueStr = spec.ValueText;
                            else
                                continue;

                            // ИСПРАВЛЕНИЕ: Используем DisplayName вместо Name
                            // Пример: "Оперативная память: 16" вместо "ram_gb: 16"
                            specPhrases.Add($"{spec.DisplayName}: {valueStr}");

                            // Добавляем значение в список ключевых слов для дублирования
                            importantKeywords.Add(valueStr);
                        }
                    }

                    // Собираем финальный текст
                    var fullTextParts = new List<string> { baseText };

                    if (specPhrases.Any())
                    {
                        // Добавляем блок характеристик в конец
                        fullTextParts.Add("Характеристики: " + string.Join("; ", specPhrases));

                        // ДУБЛИРУЕМ ключевые слова в начало текста (важно для поиска!)
                        if (importantKeywords.Any())
                        {
                            var keywordsString = string.Join(", ", importantKeywords);
                            // Вставляем сразу после базового текста
                            fullTextParts[0] = $"{baseText}, Ключевые особенности: {keywordsString}";
                        }
                    }

                    var fullText = string.Join(". ", fullTextParts);

                    if (string.IsNullOrWhiteSpace(fullText))
                    {
                        _logger.LogWarning("Товар ID {ProductId} пропускается (пустой текст).", product.ProductId);
                        continue;
                    }

                    // Логируем пример текста (первых 200 символов)
                    _logger.LogDebug("Текст для векторизации (ID {Id}): {Text}", product.ProductId, fullText.Length > 200 ? fullText.Substring(0, 200) + "..." : fullText);

                    // 4. Генерируем вектор
                    var vector = await embeddingService.GenerateEmbeddingAsync(fullText, cancellationToken);

                    if (vector.Length != ExpectedDimension)
                    {
                        _logger.LogError("Неверная размерность вектора для товара ID {ProductId}.", product.ProductId);
                        continue;
                    }

                    var vectorBytes = new byte[vector.Length * sizeof(float)];
                    Buffer.BlockCopy(vector, 0, vectorBytes, 0, vectorBytes.Length);

                    // 5. Формируем HashEntry
                    var hashEntries = new List<HashEntry>
                    {
                        new HashEntry("name", product.ProductName ?? ""),
                        new HashEntry("description", product.ProductDescription ?? ""),
                        new HashEntry("embedding", vectorBytes),
                        new HashEntry("category", categoryName),
                        new HashEntry("price", ((double)product.ProductPrice).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        new HashEntry("stock", product.ProductStockQuantity.ToString()),
                        new HashEntry("availability", product.ProductAvailability ? "InStock" : "OutOfStock"),
                        new HashEntry("image_url", product.ImageUrl ?? "")
                    };

                    // Добавляем характеристики в Redis (используем техническое имя как ключ, значение как есть)
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

                    // 6. Сохраняем в Redis
                    await db.HashSetAsync($"product:{product.ProductId}", hashEntries.ToArray());
                    _logger.LogDebug("Товар ID {ProductId} сохранён в Redis.", product.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при индексации товара ID {ProductId}", product.ProductId);
                }
            }

            _logger.LogInformation("Все Hash-ключи товара созданы. Начинаем создание индекса...");
            // --- КОНЕЦ ШАГА 2 ---

            await CreateIndexAsync();

            _logger.LogInformation("Индексация завершена успешно. Индекс обновлен.");
        }

        // --- НОВЫЙ МЕТОД: Создание индекса ---
        private async Task CreateIndexAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            try
            {
                // --- НОВОЕ: Получаем список всех уникальных имен характеристик из базы данных ---
                // Создаем временный scope для получения репозитория
                using var scope = _scopeFactory.CreateScope();
                var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                // Загружаем все связи (ProductSpecLinks) -> (Spec) -> (Value)
                // и извлекаем уникальные имена характеристик (Spec.Name)
                var allSpecNames = await productRepository.GetAllProductSpecificationsRawAsync(default);
                var uniqueSpecNames = allSpecNames
                    .Select(s => s.Name) // Берем техническое имя характеристики
                    .Distinct()
                    .ToList();

                _logger.LogInformation("Найдено уникальных характеристик для индексации: {Count}. Список: {@SpecNames}", uniqueSpecNames.Count, uniqueSpecNames);

                // Определяем типы для характеристик. Пока жестко задаем основные, остальные как TAG.
                // В реальном проекте лучше хранить тип в ProductSpecification.DataType и использовать его.
                var knownSpecTypes = new Dictionary<string, string>
                {
                    { "ram_gb", "NUMERIC" },
                    { "storage_gb", "NUMERIC" },
                    { "refresh_rate_hz", "NUMERIC" },
                    { "screen_size_inch", "NUMERIC" },
                    { "weight_g", "NUMERIC" },
                    { "weight_kg", "NUMERIC" },
                    // ... добавьте другие числовые характеристики ...
                };

                // --- Формируем аргументы для FT.CREATE ---
                var args = new List<object>
                {
                    "idx:products",
                    "ON", "HASH",
                    "PREFIX", "1", "product:",
                    "SCHEMA",
                    // Стандартные поля
                    "name", "TEXT", "WEIGHT", "5.0", // Пример веса
                    "description", "TEXT", "WEIGHT", "1.0",
                    "category", "TAG", "SEPARATOR", ";",
                    "price", "NUMERIC",
                    "stock", "NUMERIC",
                    "availability", "TAG",
                    "image_url", "TEXT",
                    // Векторное поле
                    "embedding", "VECTOR", "FLAT", "6",
                    "TYPE", "FLOAT32",
                    "DIM", "768", // Указываем константу напрямую
                    "DISTANCE_METRIC", "COSINE"
                };

                // --- Добавляем характеристики в схему ---
                foreach (var specName in uniqueSpecNames)
                {
                    string fieldType = knownSpecTypes.GetValueOrDefault(specName, "TAG"); // По умолчанию TAG
                    args.Add(specName);
                    args.Add(fieldType);
                    // Если тип TAG, добавим SEPARATOR, если планируете хранить несколько значений через ; в одном поле
                    if (fieldType == "TAG")
                    {
                        args.Add("SEPARATOR");
                        args.Add(";"); // Убедитесь, что это соответствует вашему способу хранения в Hash
                    }
                }
                // --- КОНЕЦ ДОБАВЛЕНИЯ ХАРАКТЕРИСТИК ---

                // --- Выполняем создание индекса ---
                _logger.LogInformation("Создание индекса Redis с характеристиками...");
                await server.ExecuteAsync("FT.CREATE", args.ToArray());

                _logger.LogInformation("Векторный индекс 'idx:products' создан/обновлен с размерностью 768 и включением характеристик.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании векторного индекса.");
                throw; // Перебрасываем исключение, чтобы остановить службу или обработать в вызывающем коде
            }
        }
        // --- КОНЕЦ НОВОГО МЕТОДА ---
    }
}