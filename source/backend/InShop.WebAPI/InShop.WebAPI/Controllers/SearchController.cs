using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly ConnectionMultiplexer _redis;
        private readonly ILogger<SearchController> _logger;

        // Порог для косинусного расстояния (0.0 = максимально близкие, 2.0 = максимально далекие)
        private const double MaxCosineDistanceThreshold = 0.7;

        public SearchController(IEmbeddingService embeddingService, ConnectionMultiplexer redis, ILogger<SearchController> logger)
        {
            _embeddingService = embeddingService;
            _redis = redis;
            _logger = logger;
        }

        [HttpGet("vector-search")]
        public async Task<IActionResult> SearchVector(
            [FromQuery] string q,
            [FromQuery] int limit = 100,
            [FromQuery] string? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Параметр поиска 'q' обязателен.");
            }

            _logger.LogInformation("Получен векторный поиск: '{Query}', Limit: {Limit}", q, limit);

            try
            {
                // 1. Генерируем вектор для запроса
                var queryVector = await _embeddingService.GenerateEmbeddingAsync(q, ct);

                // 2. Конвертируем вектор в байты для Redis
                var queryVectorBytes = new byte[queryVector.Length * sizeof(float)];
                Buffer.BlockCopy(queryVector, 0, queryVectorBytes, 0, queryVectorBytes.Length);

                // 3. Подготовим фильтр (если есть)
                var filterPart = BuildFilterClause(category, minPrice, maxPrice);
                // Если фильтр пустой, используем *
                var baseQuery = string.IsNullOrWhiteSpace(filterPart) ? "*" : filterPart;

                // 4. ИСПРАВЛЕНО: формируем команду FT.SEARCH с правильным синтаксисом KNN
                // Включаем AS vector_score и LOAD для получения расстояния
                var query = $"{baseQuery}=>[KNN {limit} @embedding $BLOB AS vector_score]";

                _logger.LogDebug("Формируемая строка запроса для FT.SEARCH: '{Query}'", query);

                var db = _redis.GetDatabase();

                var result = await db.ExecuteAsync("FT.SEARCH",
                    "idx:products",           // индекс
                    query,                    // запрос с KNN и AS vector_score
                    "PARAMS", "2", "BLOB", queryVectorBytes,  // параметры
                    "RETURN", "8", "name", "description", "price", "category", "stock", "availability", "image_url", "vector_score", // <--- Добавлено "vector_score", увеличено количество на 8
                    "SORTBY", "vector_score",  // сортировка по векторной оценке (расстоянию)
                    "LIMIT", "0", limit,       // лимит
                    "DIALECT", "2"             // dialect 2 обязателен для KNN
                );

                // ИСПРАВЛЕНО: обрабатываем результат
                return await ProcessSearchResult(result, q);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении векторного поиска для запроса: '{Query}'", q);
                return StatusCode(500, "Внутренняя ошибка сервера при выполнении поиска.");
            }
        }

        // ИСПРАВЛЕНО: новый метод для обработки результата
        private async Task<IActionResult> ProcessSearchResult(RedisResult rawResult, string query)
        {
            _logger.LogDebug("Получен сырой результат из Redis: {RawResultType}", rawResult.Resp2Type);

            // ИСПОЛЬЗУЕМ Resp2Type
            if (rawResult.Resp2Type != ResultType.Array)
            {
                _logger.LogError("Ожидался Array результат от FT.SEARCH, получен: {ResultType}", rawResult.Resp2Type);
                return StatusCode(500, "Внутренняя ошибка сервера: неверный формат результата от поиска.");
            }

            var response = (RedisResult[])rawResult;

            if (response.Length < 1)
            {
                _logger.LogError("Неверный формат ответа FT.SEARCH: недостаточно элементов. Length: {Length}", response.Length);
                return StatusCode(500, "Внутренняя ошибка сервера: неверный формат результата поиска.");
            }

            var totalResults = (long)response[0];
            _logger.LogDebug("Всего результатов по запросу: {TotalResults}", totalResults);

            // --- ПОСТ-ПРОВЕРКА РЕЛЕВАНТНОСТИ ---
            if (totalResults > 0 && response.Length >= 2)
            {
                var firstResultFields = response[2]; // Второй результат: [key, [fields_array_with_vector_score]]

                // Проверяем, что первый результат - это массив полей
                if (firstResultFields.Resp2Type == ResultType.Array)
                {
                    var fieldsArray = (RedisResult[])firstResultFields;
                    // Ищем vector_score в массиве полей
                    double? firstResultDistance = null;
                    for (int i = 0; i < fieldsArray.Length; i += 2)
                    {
                        // Проверяем тип для имени поля
                        if (fieldsArray[i].Resp2Type == ResultType.BulkString && (string)fieldsArray[i] == "vector_score" || (string)fieldsArray[i] == "__v_score")
                        {
                            // Проверяем тип для значения поля
                            if (i + 1 < fieldsArray.Length && fieldsArray[i + 1].Resp2Type == ResultType.BulkString)
                            {
                                var scoreStr = (string)fieldsArray[i + 1];
                                if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double score))
                                {
                                    firstResultDistance = score;
                                    break;
                                }
                                else
                                {
                                    _logger.LogWarning("Не удалось распарсить значение 'vector_score': {ScoreStr}", scoreStr);
                                }
                            }
                        }
                    }

                    if (firstResultDistance.HasValue)
                    {
                        _logger.LogDebug("Расстояние до наиболее близкого товара: {Distance}", firstResultDistance.Value);

                        // Сравниваем с порогом
                        if (firstResultDistance.Value > MaxCosineDistanceThreshold)
                        {
                            _logger.LogInformation("Запрос '{Query}' не релевантен (расстояние {Distance} > порог {Threshold}). Возвращаем пустой результат.", query, firstResultDistance.Value, MaxCosineDistanceThreshold);
                            // Возвращаем пустой список, если расстояние больше порога
                            return Ok(new List<ProductSearchResultDto>());
                        }
                        else
                        {
                            _logger.LogDebug("Расстояние {Distance} <= порога {Threshold}. Продолжаем обработку результатов.", firstResultDistance.Value, MaxCosineDistanceThreshold);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Не удалось извлечь 'vector_score' из первого результата. Продолжаем обработку без проверки релевантности.");
                    }
                }
                else
                {
                    _logger.LogWarning("Поля первого результата не являются массивом. Продолжаем обработку без проверки релевантности.");
                }
            }
            else if (totalResults == 0)
            {
                _logger.LogInformation("Найдено 0 результатов для запроса: '{Query}'", query);
                return Ok(new List<ProductSearchResultDto>()); // Уже пустой список
            }
            else
            {
                _logger.LogWarning("Недостаточно элементов в результатах для проверки релевантности. Продолжаем обработку.");
            }
            // --- КОНЕЦ ПОСТ-ПРОВЕРКИ ---

            var products = new List<ProductSearchResultDto>();

            // ИСПРАВЛЕНО: правильная обработка пар ключ-значение
            // i начинается с 1, так как response[0] - общее количество
            for (int i = 1; i < response.Length; i++)
            {
                // Проверяем, не вышли ли за границы
                if (i >= response.Length) break;

                var key = (string)response[i];
                i++; // Переходим к следующему элементу (массиву полей)

                // Проверяем, не вышли ли за границы после инкремента
                if (i >= response.Length) break;

                var fieldsResult = response[i];

                // Проверяем тип массива полей
                if (fieldsResult.Resp2Type != ResultType.Array)
                {
                    _logger.LogWarning("Неверный тип элемента результата (поля): {FieldsType}", fieldsResult.Resp2Type);
                    continue; // Пропускаем некорректный результат
                }

                var fields = (RedisResult[])fieldsResult; // Приведение к массиву полей

                var product = ParseProductFromFields(key, fields);
                if (product != null)
                {
                    products.Add(product);
                }
            }

            _logger.LogInformation("Найдено {Count} результатов для запроса: '{Query}'", products.Count, query);
            return Ok(products);
        }

        // ИСПРАВЛЕНО: новый метод для парсинга продукта
        private ProductSearchResultDto? ParseProductFromFields(string key, RedisResult[] fields)
        {
            try
            {
                var fieldDict = new Dictionary<string, string>();

                // Поля приходят парами: [fieldName, fieldValue, fieldName, fieldValue, ...]
                // Используем Resp2Type для проверки типов
                for (int j = 0; j < fields.Length; j += 2)
                {
                    if (j + 1 >= fields.Length) break;

                    // Проверяем тип имени поля
                    if (fields[j].Resp2Type != ResultType.BulkString)
                    {
                        _logger.LogWarning("Неверный тип имени поля в результатах для товара {Key}. Тип: {FieldType}", key, fields[j].Resp2Type);
                        continue; // Пропускаем эту пару
                    }
                    var fieldName = (string)fields[j];

                    // Проверяем тип значения поля
                    if (fields[j + 1].Resp2Type != ResultType.BulkString)
                    {
                        _logger.LogWarning("Неверный тип значения поля '{FieldName}' в результатах для товара {Key}. Тип: {FieldType}", fieldName, key, fields[j + 1].Resp2Type);
                        continue; // Пропускаем эту пару
                    }
                    var fieldValue = (string)fields[j + 1];


                    if (fieldName != null && fieldValue != null)
                    {
                        // Пропускаем служебное поле vector_score при парсинге DTO
                        if (fieldName != "vector_score")
                        {
                            fieldDict[fieldName] = fieldValue;
                        }
                    }
                }

                return new ProductSearchResultDto
                {
                    Id = ExtractIdFromKey(key),
                    Name = fieldDict.GetValueOrDefault("name", string.Empty),
                    Description = fieldDict.GetValueOrDefault("description", string.Empty),
                    Price = decimal.TryParse(fieldDict.GetValueOrDefault("price", ""), out var price) ? price : 0,
                    Category = fieldDict.GetValueOrDefault("category", string.Empty),
                    StockQuantity = int.TryParse(fieldDict.GetValueOrDefault("stock", ""), out var stock) ? stock : 0,
                    IsAvailable = fieldDict.GetValueOrDefault("availability") == "InStock",
                    ImageUrl = fieldDict.GetValueOrDefault("image_url", string.Empty),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге продукта с ключом: {Key}", key);
                return null;
            }
        }

        private static string BuildFilterClause(string? category, decimal? minPrice, decimal? maxPrice)
        {
            var clauses = new List<string>();

            if (!string.IsNullOrEmpty(category))
            {
                clauses.Add($"@category:{{{category}}}");
            }

            if (minPrice.HasValue)
            {
                clauses.Add($"@price:[{minPrice.Value} +inf]");
            }

            if (maxPrice.HasValue)
            {
                clauses.Add($"@price:[-inf {maxPrice.Value}]");
            }

            // ИСПРАВЛЕНО: если есть несколько условий, объединяем их через пробел
            return clauses.Count > 0 ? string.Join(" ", clauses) : "*";
        }

        private static int ExtractIdFromKey(string key)
        {
            // Добавьте проверку на наличие разделителя
            var lastColon = key.LastIndexOf(':');
            if (lastColon >= 0 && lastColon < key.Length - 1)
            {
                return int.Parse(key.Substring(lastColon + 1));
            }
            throw new ArgumentException($"Невозможно извлечь ID из ключа: {key}");
        }
    }
}