using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private const double MaxCosineDistanceThreshold = 0.5;

        // Веса для гибридного поиска
        private const double VectorWeight = 0.4; // Вес векторной оценки (1 - distance)
        private const double LexicalWeight = 0.6; // Вес лексической оценки (BM25)

        public SearchController(IEmbeddingService embeddingService, ConnectionMultiplexer redis, ILogger<SearchController> logger)
        {
            _embeddingService = embeddingService;
            _redis = redis;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchVector(
            [FromQuery] string q,
            [FromQuery] int limit = 100,
            [FromQuery] string? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? inStock = null, // Добавляем параметр inStock
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Параметр поиска 'q' обязателен.");
            }

            _logger.LogInformation("Получен гибридный поиск: '{Query}', Limit: {Limit}, Category: '{Category}', MinPrice: {MinPrice}, MaxPrice: {MaxPrice}, InStock: {InStock}",
                q, limit, category ?? "null", minPrice?.ToString() ?? "null", maxPrice?.ToString() ?? "null", inStock?.ToString() ?? "null");

            try
            {
                // 1. Генерируем вектор для запроса
                var queryVector = await _embeddingService.GenerateEmbeddingAsync(q, ct);

                // 2. Конвертируем вектор в байты для Redis
                var queryVectorBytes = new byte[queryVector.Length * sizeof(float)];
                Buffer.BlockCopy(queryVector, 0, queryVectorBytes, 0, queryVectorBytes.Length);

                // 3. Подготовим фильтр (с учетом нового параметра inStock)
                var filterClause = BuildFilterClause(category, minPrice, maxPrice, inStock);

                // 4. Формируем команды для векторного и лексического поиска
                var vectorQuery = string.IsNullOrEmpty(filterClause)
                    ? $"*=>[KNN {limit} @embedding $BLOB AS vector_distance]"
                    : $"({filterClause})=>[KNN {limit} @embedding $BLOB AS vector_distance]";

                var escapedQueryTerms = string.Join(" ", q.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                var lexicalQuery = string.IsNullOrEmpty(filterClause)
                    ? escapedQueryTerms
                    : $"({filterClause}) {escapedQueryTerms}";

                _logger.LogDebug("Формируемая строка векторного запроса для FT.SEARCH: '{Query}'", vectorQuery);
                _logger.LogDebug("Формируемая строка лексического запроса для FT.SEARCH: '{Query}'", lexicalQuery);

                var db = _redis.GetDatabase();

                // --- ВЫПОЛНЯЕМ ВЕКТОРНЫЙ ПОИСК ---
                var vectorResultTask = db.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    vectorQuery,
                    "PARAMS", "2", "BLOB", (RedisValue)queryVectorBytes,
                    "RETURN", "8", "name", "description", "price", "category", "stock", "availability", "image_url", "vector_distance",
                    "SORTBY", "vector_distance",
                    "ASC",
                    "LIMIT", "0", limit.ToString(),
                    "DIALECT", "4"
                );

                // --- ВЫПОЛНЯЕМ ЛЕКСИЧЕСКИЙ ПОИСК С ИСПОЛЬЗОВАНИЕМ WITHSCORES ---
                var lexicalResultTask = db.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    lexicalQuery,
                    "SCORER", "BM25",
                    "WITHSCORES",
                    "RETURN", "7", "name", "description", "price", "category", "stock", "availability", "image_url",
                    "LIMIT", "0", limit.ToString(),
                    "DIALECT", "4"
                );

                await Task.WhenAll(vectorResultTask, lexicalResultTask);

                var vectorResult = await vectorResultTask;
                var lexicalResult = await lexicalResultTask;

                // --- ОБРАБАТЫВАЕМ РЕЗУЛЬТАТЫ ---
                var hybridResults = await ProcessHybridSearch(vectorResult, lexicalResult, MaxCosineDistanceThreshold);
                var sortedResults = hybridResults.OrderByDescending(x => x.HybridScore).ToList();

                var finalDtoList = sortedResults.Select(r => r.Dto).Take(limit).ToList();

                _logger.LogInformation("Найдено {Count} гибридных результатов для запроса: '{Query}'", finalDtoList.Count, q);

                return Ok(finalDtoList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении гибридного поиска для запроса: '{Query}'", q);
                return StatusCode(500, "Внутренняя ошибка сервера при выполнении поиска.");
            }
        }

        // --- НОВЫЙ МЕТОД: Обработка гибридного поиска ---
        private async Task<List<HybridResult>> ProcessHybridSearch(RedisResult vectorResult, RedisResult lexicalResult, double threshold)
        {
            // Векторный результат: ожидаем, что оценка (vector_distance) внутри массива полей
            var vectorProducts = ParseRedisResultToDict(vectorResult, "vector_distance", ScoreLocation.InArray, true, threshold);

            // Лексический результат: ожидаем, что оценка (__score) после ключа, благодаря WITHSCORES
            var lexicalProducts = ParseRedisResultToDict(lexicalResult, "__score", ScoreLocation.AfterKey, false, null); // Лексический поиск не фильтруем по порогу расстояния

            var hybridResults = new Dictionary<int, HybridResult>();

            // Обрабатываем векторные результаты
            foreach (var kvp in vectorProducts)
            {
                var id = kvp.Key;
                var dto = kvp.Value.dto;
                var vectorDistance = kvp.Value.score; // Это расстояние (0..2)

                if (!hybridResults.ContainsKey(id))
                {
                    hybridResults[id] = new HybridResult { Dto = dto };
                }
                // Преобразуем расстояние в оценку (чем меньше расстояние, тем выше оценка)
                // Косинусное сходство = 1 - косинусное_расстояние
                var vectorSimilarity = 1.0 - vectorDistance;
                hybridResults[id].VectorScore = vectorSimilarity;
            }

            // Обрабатываем лексические результаты
            foreach (var kvp in lexicalProducts)
            {
                var id = kvp.Key;
                var dto = kvp.Value.dto;
                var lexicalScore = kvp.Value.score; // Это BM25 оценка

                if (!hybridResults.ContainsKey(id))
                {
                    hybridResults[id] = new HybridResult { Dto = dto };
                }
                hybridResults[id].LexicalScore = lexicalScore;
            }

            // Рассчитываем гибридную оценку
            foreach (var hybridResult in hybridResults.Values)
            {
                var vScore = hybridResult.VectorScore ?? 0.0;
                var lScore = hybridResult.LexicalScore ?? 0.0;
                hybridResult.HybridScore = (VectorWeight * vScore) + (LexicalWeight * lScore);
            }

            return hybridResults.Values.ToList();
        }

        // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Парсинг RedisResult в словарь ---
        private Dictionary<int, (ProductSearchResultDto dto, double score)> ParseRedisResultToDict(RedisResult rawResult, string scoreFieldName, ScoreLocation expectedScoreLocation, bool isVector, double? threshold)
        {
            var dict = new Dictionary<int, (ProductSearchResultDto dto, double score)>();

            if (rawResult.Resp2Type != ResultType.Array)
            {
                _logger.LogWarning("Результат поиска не является массивом. Тип: {Type}", rawResult.Resp2Type);
                return dict;
            }

            var response = (RedisResult[])rawResult;
            if (response.Length < 1)
            {
                _logger.LogWarning("Результат поиска пуст или содержит недостаточно данных.");
                return dict;
            }

            // Предполагаем, что response[0] - это общее количество результатов
            // Начинаем парсинг результатов с индекса 1
            for (int i = 1; i < response.Length; /* i увеличивается внутри */ )
            {
                // 1. Определяем, является ли элемент ключом товара
                if (i >= response.Length || response[i].Resp2Type != ResultType.BulkString)
                {
                    _logger.LogWarning("Ожидался ключ товара (BulkString) на позиции {Pos}, получен: {Type}. Прерывание парсинга.", i, response[i].Resp2Type);
                    break; // Нарушена структура, выходим
                }
                var key = (string)response[i];
                i++; // Перешли к следующему элементу после ключа

                double? score = null;
                Dictionary<string, string> fieldDict = new Dictionary<string, string>();

                // 2. Проверяем ожидаемую структуру на основе expectedScoreLocation
                switch (expectedScoreLocation)
                {
                    case ScoreLocation.InArray:
                        // Структура: [key, [field1, val1, score_field, score_val, field2, val2, ...]]
                        if (i >= response.Length || response[i].Resp2Type != ResultType.Array)
                        {
                            _logger.LogWarning("Ожидался массив полей для товара {Key} после ключа, получен: {Type}. Пропуск.", key, response[i].Resp2Type);
                            i++; // Сдвигаемся, чтобы не зависнуть
                            continue;
                        }

                        var fieldsArray_InArray = (RedisResult[])response[i];
                        i++; // Перешли к следующему результату (ключ)

                        // Парсим поля внутри массива
                        for (int j = 0; j < fieldsArray_InArray.Length; j += 2)
                        {
                            if (j + 1 >= fieldsArray_InArray.Length)
                            {
                                _logger.LogWarning("Нечетное количество элементов в массиве полей для товара {Key}.", key);
                                break; // Нарушена структура полей
                            }

                            var fieldNameResult = fieldsArray_InArray[j];
                            var fieldValueResult = fieldsArray_InArray[j + 1];

                            if (fieldNameResult.Resp2Type != ResultType.BulkString || fieldValueResult.Resp2Type != ResultType.BulkString)
                            {
                                _logger.LogWarning("Неверный тип имени или значения поля для товара {Key}. FieldNameType: {FNType}, FieldValueType: {FVType}", key, fieldNameResult.Resp2Type, fieldValueResult.Resp2Type);
                                continue; // Пропускаем это поле
                            }

                            var fieldName = (string)fieldNameResult;
                            var fieldValue = (string)fieldValueResult;

                            if (fieldName == scoreFieldName)
                            {
                                // Пытаемся распарсить как оценку
                                if (double.TryParse(fieldValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double s))
                                {
                                    score = s;
                                    _logger.LogDebug("Найдена оценка '{ScoreField}' ({ScoreVal}) внутри массива полей для товара {Key}", scoreFieldName, s, key);
                                }
                                else
                                {
                                    _logger.LogWarning("Поле '{ScoreField}' для товара {Key} ({FieldValue}) не является числом. Игнорируется.", scoreFieldName, key, fieldValue);
                                    // Продолжаем обработку, считая это обычным полем
                                    fieldDict[fieldName] = fieldValue;
                                }
                            }
                            else
                            {
                                // Это обычное поле
                                fieldDict[fieldName] = fieldValue;
                            }
                        }
                        break;

                    case ScoreLocation.AfterKey:
                        // Структура: [key, score_value, [field1, val1, field2, val2, ...]]
                        if (i >= response.Length || response[i].Resp2Type != ResultType.BulkString)
                        {
                            _logger.LogWarning("После ключа {Key} ожидалась оценка (BulkString), получен: {Type}. Пропуск.", key, response[i].Resp2Type);
                            i++; // Сдвигаемся, чтобы не зависнуть
                            continue;
                        }

                        var possibleScoreValue_AfterKey = (string)response[i];
                        if (!double.TryParse(possibleScoreValue_AfterKey, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double extractedScore))
                        {
                            _logger.LogWarning("После ключа {Key} ожидалась числовая оценка, получено: '{Value}'. Пропуск.", key, possibleScoreValue_AfterKey);
                            i++; // Сдвигаемся, чтобы не зависнуть
                            continue;
                        }
                        score = extractedScore;
                        i++; // Перешли к массиву полей

                        if (i >= response.Length || response[i].Resp2Type != ResultType.Array)
                        {
                            _logger.LogWarning("После оценки для товара {Key} ожидался массив полей, получен: {Type}", key, response[i]?.Resp2Type ?? ResultType.None);
                            i--; // Вернемся, чтобы не потерять элемент
                            continue; // Пропускаем
                        }

                        var fieldsArray_AfterKey = (RedisResult[])response[i];
                        i++; // Перешли к следующему результату (ключ)

                        // Парсим поля, как в сценарии A, но оценка уже есть
                        for (int j = 0; j < fieldsArray_AfterKey.Length; j += 2)
                        {
                            if (j + 1 >= fieldsArray_AfterKey.Length)
                            {
                                _logger.LogWarning("Нечетное количество элементов в массиве полей для товара {Key}.", key);
                                break;
                            }

                            var fieldNameResult = fieldsArray_AfterKey[j];
                            var fieldValueResult = fieldsArray_AfterKey[j + 1];

                            if (fieldNameResult.Resp2Type != ResultType.BulkString || fieldValueResult.Resp2Type != ResultType.BulkString)
                            {
                                _logger.LogWarning("Неверный тип имени или значения поля для товара {Key}. FieldNameType: {FNType}, FieldValueType: {FVType}", key, fieldNameResult.Resp2Type, fieldValueResult.Resp2Type);
                                continue;
                            }

                            var fieldName = (string)fieldNameResult;
                            var fieldValue = (string)fieldValueResult;

                            // В этом сценарии __score уже извлечен, просто добавляем в словарь
                            fieldDict[fieldName] = fieldValue;
                        }
                        break;
                }

                // Если оценка так и не была найдена
                if (!score.HasValue)
                {
                    _logger.LogWarning("Оценка для товара {Key} не найдена или не является числом. Пропуск.", key);
                    continue; // Пропускаем этот результат
                }

                // --- Фильтр по порогу (только для векторного расстояния) ---
                if (isVector && threshold.HasValue && score.Value > threshold.Value)
                {
                    _logger.LogDebug("Товар {Key} имеет расстояние {Distance} > порога {Threshold}, пропускаем.", key, score.Value, threshold.Value);
                    continue;
                }

                // --- Создание DTO ---
                var product = CreateDtoFromFields(key, fieldDict);
                dict[product.Id] = (product, score.Value);
            }

            return dict;
        }

        // --- Вспомогательный метод для создания DTO из словаря ---
        private ProductSearchResultDto CreateDtoFromFields(string key, Dictionary<string, string> fieldDict)
        {
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

        private static string BuildFilterClause(string? category, decimal? minPrice, decimal? maxPrice, bool? inStock)
        {
            var clauses = new List<string>();

            // Для категории (TAG field)
            if (!string.IsNullOrEmpty(category))
            {
                var escapedCategory = EscapeTagValue(category);
                clauses.Add($"@category:{{{escapedCategory}}}");
            }

            // Для цены (NUMERIC field)
            if (minPrice.HasValue && maxPrice.HasValue)
            {
                clauses.Add($"@price:[{minPrice.Value} {maxPrice.Value}]");
            }
            else if (minPrice.HasValue)
            {
                clauses.Add($"@price:[{minPrice.Value} +inf]");
            }
            else if (maxPrice.HasValue)
            {
                clauses.Add($"@price:[-inf {maxPrice.Value}]");
            }

            // Для наличия (TAG field - availability)
            if (inStock.HasValue)
            {
                // В Redis availability хранится как "InStock" или "OutOfStock"
                var availabilityValue = inStock.Value ? "InStock" : "OutOfStock";
                clauses.Add($"@availability:{{{availabilityValue}}}");
            }

            var result = string.Join(" ", clauses);

            return result;
        }

        private static string EscapeTagValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = value;

            // Экранируем специальные символы для TAG полей в RedisSearch
            // Список символов для экранирования: , . < > { } [ ] " ' : ; ! @ # $ % ^ & * ( ) - + = ~ | / \ ? и пробел
            var specialChars = new[] { ',', '.', '<', '>', '{', '}', '[', ']', '"', '\'', ':', ';', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '~', '|', '/', '\\', '?', ' ' };

            foreach (var c in specialChars)
            {
                result = result.Replace(c.ToString(), "\\" + c);
            }

            return result;
        }

        private static int ExtractIdFromKey(string key)
        {
            var lastColon = key.LastIndexOf(':');
            if (lastColon >= 0 && lastColon < key.Length - 1)
            {
                return int.Parse(key.Substring(lastColon + 1));
            }
            throw new ArgumentException($"Невозможно извлечь ID из ключа: {key}");
        }

        // --- ВСПОМОГАТЕЛЬНЫЙ КЛАСС ДЛЯ ХРАНЕНИЯ ГИБРИДНОГО РЕЗУЛЬТАТА ---
        private class HybridResult
        {
            public ProductSearchResultDto Dto { get; set; } = null!;
            public double? VectorScore { get; set; }
            public double? LexicalScore { get; set; }
            public double HybridScore { get; set; }
        }

        // --- ENUM для указания места ожидаемой оценки ---
        public enum ScoreLocation { InArray, AfterKey }
    }
}