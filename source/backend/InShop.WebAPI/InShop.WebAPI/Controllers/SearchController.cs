using Contracts.Dtos; // Добавьте using
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
        private readonly IProductService _productService; // Внедрите через конструктор

        // Порог для косинусного расстояния (0.0 = максимально близкие, 2.0 = максимально далекие)
        private const double MaxCosineDistanceThreshold = 0.5;

        // Веса для гибридного поиска
        private const double VectorWeight = 0.4; // Вес векторной оценки (1 - distance)
        private const double LexicalWeight = 0.6; // Вес лексической оценки (BM25)

        public SearchController(
            IEmbeddingService embeddingService,
            ConnectionMultiplexer redis,
            ILogger<SearchController> logger,
            IProductService productService) // <-- Добавлен IProductService
        {
            _embeddingService = embeddingService;
            _redis = redis;
            _logger = logger;
            _productService = productService; // <-- Присвоено
        }

        // ... остальные методы, например, GetSpecificationFiltersForCategory ...

        [HttpPost("search")] // <-- Изменили на POST и принимаем DTO в теле
        public async Task<IActionResult> SearchVectorPost(
            [FromBody] SearchRequestDto request, // <-- Принимаем DTO из тела запроса
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Параметр поиска 'q' обязателен.");
            }

            // Валидация параметров сортировки
            if (!IsValidSortParameter(request.SortBy, out string validatedSortBy))
            {
                return BadRequest($"Недопустимое значение параметра 'sortBy': '{request.SortBy}'. Допустимые значения: relevance, name, price.");
            }

            if (!IsValidSortOrder(request.SortOrder, out string validatedSortOrder))
            {
                return BadRequest($"Недопустимое значение параметра 'sortOrder': '{request.SortOrder}'. Допустимые значения: asc, desc.");
            }

            // ВАЖНО: Валидация фильтров по характеристикам
            Dictionary<string, object>? validatedSpecFilters = null;
            if (request.SpecFilters != null && !string.IsNullOrEmpty(request.Category))
            {
                validatedSpecFilters = await _productService.ValidateSpecFiltersAsync(request.SpecFilters, request.Category);
                if (validatedSpecFilters == null)
                {
                    _logger.LogWarning("Фильтры по характеристикам недопустимы для категории '{Category}' или имеют неверный тип. Запрос: {@SpecFilters}", request.Category, request.SpecFilters);
                    return BadRequest("Один или несколько фильтров по характеристикам недопустимы для выбранной категории или имеют неверный тип.");
                }
            }
            else if (request.SpecFilters != null && string.IsNullOrEmpty(request.Category))
            {
                _logger.LogInformation("Фильтры по характеристикам переданы, но категория не выбрана. Фильтры игнорируются.");
                validatedSpecFilters = null;
            }


            _logger.LogInformation("Получен гибридный поиск: '{Query}', Limit: {Limit}, Category: '{Category}', MinPrice: {MinPrice}, MaxPrice: {MaxPrice}, InStock: {InStock}, SortBy: {SortBy}, SortOrder: {SortOrder}",
                request.Query, request.Limit, request.Category ?? "null", request.MinPrice?.ToString() ?? "null", request.MaxPrice?.ToString() ?? "null", request.InStock?.ToString() ?? "null", validatedSortBy, validatedSortOrder);


            try
            {
                // 1. Генерируем вектор для запроса
                var queryVector = await _embeddingService.GenerateEmbeddingAsync(request.Query, ct);

                // 2. Конвертируем вектор в байты для Redis
                var queryVectorBytes = new byte[queryVector.Length * sizeof(float)];
                Buffer.BlockCopy(queryVector, 0, queryVectorBytes, 0, queryVectorBytes.Length);

                // 3. Подготовим фильтр
                var filterClause = BuildFilterClause(request.Category, request.MinPrice, request.MaxPrice, request.InStock, validatedSpecFilters);

                // 4. Формируем команды для векторного и лексического поиска
                var vectorQuery = string.IsNullOrEmpty(filterClause)
                    ? $"*=>[KNN {request.Limit} @embedding $BLOB AS vector_distance]"
                    : $"({filterClause})=>[KNN {request.Limit} @embedding $BLOB AS vector_distance]";

                var escapedQueryTerms = string.Join(" ", request.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                var lexicalQuery = string.IsNullOrEmpty(filterClause)
                    ? escapedQueryTerms
                    : $"({filterClause}) {escapedQueryTerms}";

                _logger.LogInformation("Формируемая строка векторного запроса для FT.SEARCH: '{Query}'", vectorQuery);
                _logger.LogInformation("Формируемая строка лексического запроса для FT.SEARCH: '{Query}'", lexicalQuery);

                var db = _redis.GetDatabase();

                // --- ВЫПОЛНЯЕМ ВЕКТОРНЫЙ ПОИСК ---
                var vectorResultTask = db.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    vectorQuery,
                    "PARAMS", "2", "BLOB", (RedisValue)queryVectorBytes,
                    "RETURN", "8", "name", "description", "price", "category", "stock", "availability", "image_url", "vector_distance",
                    "SORTBY", "vector_distance",
                    "ASC",
                    "LIMIT", "0", request.Limit.ToString(),
                    "DIALECT", "4"
                );

                // --- ВЫПОЛНЯЕМ ЛЕКСИЧЕСКИЙ ПОИСК С ИСПОЛЬЗОВАНИЕМ WITHSCORES ---
                var lexicalResultTask = db.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    lexicalQuery,
                    "SCORER", "BM25",
                    "WITHSCORES",
                    "RETURN", "7", "name", "description", "price", "category", "stock", "availability", "image_url",
                    "LIMIT", "0", request.Limit.ToString(),
                    "DIALECT", "4"
                );

                await Task.WhenAll(vectorResultTask, lexicalResultTask);

                var vectorResult = await vectorResultTask;
                var lexicalResult = await lexicalResultTask;

                _logger.LogDebug("Результат векторного поиска: {@VectorResult}", vectorResult); // <-- Лог
                _logger.LogDebug("Результат лексического поиска: {@LexicalResult}", lexicalResult); // <-- Лог

                // --- ОБРАБАТЫВАЕМ РЕЗУЛЬТАТЫ ---
                var hybridResults = await ProcessHybridSearch(vectorResult, lexicalResult, MaxCosineDistanceThreshold);

                // --- ПРИМЕНЯЕМ СОРТИРОВКУ К ГИБРИДНЫМ РЕЗУЛЬТАТАМ ---
                var sortedResults = ApplySorting(hybridResults, validatedSortBy, validatedSortOrder);

                // --- БЕРЁМ ТОП-N РЕЗУЛЬТАТОВ И ИЗВЛЕКАЕМ DTO ---
                var finalDtoList = sortedResults.Select(r => r.Dto).Take(request.Limit).ToList();

                _logger.LogInformation("Найдено {Count} гибридных результатов для запроса: '{Query}'", finalDtoList.Count, request.Query);

                return Ok(finalDtoList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении гибридного поиска для запроса: '{Query}'", request.Query);
                return StatusCode(500, "Внутренняя ошибка сервера при выполнении поиска.");
            }
        }

        // --- МЕТОД: Валидация параметра сортировки ---
        private static bool IsValidSortParameter(string input, out string validatedOutput)
        {
            validatedOutput = input?.ToLowerInvariant() ?? string.Empty;
            return validatedOutput switch
            {
                "relevance" => true,
                "name" => true,
                "price" => true,
                _ => false
            };
        }

        // --- МЕТОД: Валидация параметра порядка ---
        private static bool IsValidSortOrder(string input, out string validatedOutput)
        {
            validatedOutput = input?.ToLowerInvariant() ?? string.Empty;
            return validatedOutput switch
            {
                "asc" => true,
                "desc" => true,
                _ => false
            };
        }

        // --- МЕТОД: Применение сортировки ---
        private List<HybridResult> ApplySorting(List<HybridResult> results, string sortBy, string sortOrder)
        {
            IOrderedEnumerable<HybridResult> orderedResults = sortBy switch
            {
                "name" => sortOrder == "asc" ? results.OrderBy(r => r.Dto.Name, StringComparer.OrdinalIgnoreCase) : results.OrderByDescending(r => r.Dto.Name, StringComparer.OrdinalIgnoreCase),
                "price" => sortOrder == "asc" ? results.OrderBy(r => r.Dto.Price) : results.OrderByDescending(r => r.Dto.Price),
                _ => sortOrder == "asc" ? results.OrderBy(r => r.HybridScore) : results.OrderByDescending(r => r.HybridScore)
            };

            return orderedResults.ToList();
        }

        // --- МЕТОД: Обработка гибридного поиска ---
        private async Task<List<HybridResult>> ProcessHybridSearch(RedisResult vectorResult, RedisResult lexicalResult, double threshold)
        {
            var vectorProducts = ParseRedisResultToDict(vectorResult, "vector_distance", ScoreLocation.InArray, true, threshold);
            var lexicalProducts = ParseRedisResultToDict(lexicalResult, "__score", ScoreLocation.AfterKey, false, null);

            var hybridResults = new Dictionary<int, HybridResult>();

            foreach (var kvp in vectorProducts)
            {
                var id = kvp.Key;
                var dto = kvp.Value.dto;
                var vectorDistance = kvp.Value.score;

                if (!hybridResults.ContainsKey(id))
                {
                    hybridResults[id] = new HybridResult { Dto = dto };
                }
                var vectorSimilarity = 1.0 - vectorDistance;
                hybridResults[id].VectorScore = vectorSimilarity;
            }

            foreach (var kvp in lexicalProducts)
            {
                var id = kvp.Key;
                var dto = kvp.Value.dto;
                var lexicalScore = kvp.Value.score;

                if (!hybridResults.ContainsKey(id))
                {
                    hybridResults[id] = new HybridResult { Dto = dto };
                }
                hybridResults[id].LexicalScore = lexicalScore;
            }

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

            for (int i = 1; i < response.Length; /* i увеличивается внутри */ )
            {
                if (i >= response.Length || response[i].Resp2Type != ResultType.BulkString)
                {
                    _logger.LogWarning("Ожидался ключ товара (BulkString) на позиции {Pos}, получен: {Type}. Прерывание парсинга.", i, response[i].Resp2Type);
                    break;
                }
                var key = (string)response[i];
                i++;

                double? score = null;
                Dictionary<string, string> fieldDict = new Dictionary<string, string>();

                switch (expectedScoreLocation)
                {
                    case ScoreLocation.InArray:
                        if (i >= response.Length || response[i].Resp2Type != ResultType.Array)
                        {
                            _logger.LogWarning("Ожидался массив полей для товара {Key} после ключа, получен: {Type}. Пропуск.", key, response[i].Resp2Type);
                            i++;
                            continue;
                        }

                        var fieldsArray_InArray = (RedisResult[])response[i];
                        i++;

                        for (int j = 0; j < fieldsArray_InArray.Length; j += 2)
                        {
                            if (j + 1 >= fieldsArray_InArray.Length)
                            {
                                _logger.LogWarning("Нечетное количество элементов в массиве полей для товара {Key}.", key);
                                break;
                            }

                            var fieldNameResult = fieldsArray_InArray[j];
                            var fieldValueResult = fieldsArray_InArray[j + 1];

                            if (fieldNameResult.Resp2Type != ResultType.BulkString || fieldValueResult.Resp2Type != ResultType.BulkString)
                            {
                                _logger.LogWarning("Неверный тип имени или значения поля для товара {Key}. FieldNameType: {FNType}, FieldValueType: {FVType}", key, fieldNameResult.Resp2Type, fieldValueResult.Resp2Type);
                                continue;
                            }

                            var fieldName = (string)fieldNameResult;
                            var fieldValue = (string)fieldValueResult;

                            if (fieldName == scoreFieldName)
                            {
                                if (double.TryParse(fieldValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double s))
                                {
                                    score = s;
                                    _logger.LogDebug("Найдена оценка '{ScoreField}' ({ScoreVal}) внутри массива полей для товара {Key}", scoreFieldName, s, key);
                                }
                                else
                                {
                                    _logger.LogWarning("Поле '{ScoreField}' для товара {Key} ({FieldValue}) не является числом. Игнорируется.", scoreFieldName, key, fieldValue);
                                    fieldDict[fieldName] = fieldValue;
                                }
                            }
                            else
                            {
                                fieldDict[fieldName] = fieldValue;
                            }
                        }
                        break;

                    case ScoreLocation.AfterKey:
                        if (i >= response.Length || response[i].Resp2Type != ResultType.BulkString)
                        {
                            _logger.LogWarning("После ключа {Key} ожидалась оценка (BulkString), получен: {Type}. Пропуск.", key, response[i].Resp2Type);
                            i++;
                            continue;
                        }

                        var possibleScoreValue_AfterKey = (string)response[i];
                        if (!double.TryParse(possibleScoreValue_AfterKey, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double extractedScore))
                        {
                            _logger.LogWarning("После ключа {Key} ожидалась числовая оценка, получено: '{Value}'. Пропуск.", key, possibleScoreValue_AfterKey);
                            i++;
                            continue;
                        }
                        score = extractedScore;
                        i++;

                        if (i >= response.Length || response[i].Resp2Type != ResultType.Array)
                        {
                            _logger.LogWarning("После оценки для товара {Key} ожидался массив полей, получен: {Type}", key, response[i]?.Resp2Type ?? ResultType.None);
                            i--;
                            continue;
                        }

                        var fieldsArray_AfterKey = (RedisResult[])response[i];
                        i++;

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

                            fieldDict[fieldName] = fieldValue;
                        }
                        break;
                }

                if (!score.HasValue)
                {
                    _logger.LogWarning("Оценка для товара {Key} не найдена или не является числом. Пропуск.", key);
                    continue;
                }

                if (isVector && threshold.HasValue && score.Value > threshold.Value)
                {
                    _logger.LogDebug("Товар {Key} имеет расстояние {Distance} > порога {Threshold}, пропускаем.", key, score.Value, threshold.Value);
                    continue;
                }

                var product = CreateDtoFromFields(key, fieldDict);
                dict[product.Id] = (product, score.Value);
            }

            return dict;
        }

        // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Создание DTO из словаря ---
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

        // --- ОБНОВЛЁННЫЙ МЕТОД: Построение фильтра ---
        private static string BuildFilterClause(string? category, decimal? minPrice, decimal? maxPrice, bool? inStock, Dictionary<string, object>? specFilters = null)
        {
            var clauses = new List<string>();

            if (!string.IsNullOrEmpty(category))
            {
                var escapedCategory = EscapeTagValue(category);
                clauses.Add($"@category:{{{escapedCategory}}}");
            }

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

            if (inStock.HasValue)
            {
                var availabilityValue = inStock.Value ? "InStock" : "OutOfStock";
                clauses.Add($"@availability:{{{availabilityValue}}}");
            }

            // --- НОВОЕ: Для характеристик ---
            if (specFilters != null && specFilters.Any())
            {
                foreach (var filter in specFilters)
                {
                    var specName = filter.Key;
                    var specValue = filter.Value;

                    if (specValue is { } objVal)
                    {
                        if (objVal.GetType().GetProperty("Min") != null || objVal.GetType().GetProperty("Max") != null)
                        {
                            var minProp = objVal.GetType().GetProperty("Min")?.GetValue(objVal) as decimal?;
                            var maxProp = objVal.GetType().GetProperty("Max")?.GetValue(objVal) as decimal?;
                            string rangeClause = "@";
                            rangeClause += specName;
                            rangeClause += ":[";
                            rangeClause += minProp?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-inf";
                            rangeClause += " ";
                            rangeClause += maxProp?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "+inf";
                            rangeClause += "]";
                            clauses.Add(rangeClause);
                        }
                        else
                        {
                            string valueStr;
                            if (objVal is decimal dec)
                                valueStr = dec.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            else if (objVal is string str)
                                valueStr = EscapeTagValue(str);
                            else
                                continue;

                            if (objVal is decimal)
                            {
                                clauses.Add($"@{specName}:[{valueStr} {valueStr}]");
                            }
                            else if (objVal is string)
                            {
                                clauses.Add($"@{specName}:{{{valueStr}}}");
                            }
                        }
                    }
                }
            }
            // --- КОНЕЦ НОВОГО ---
            var clauseStr = string.Join(" ", clauses);
            Console.WriteLine($"DEBUG BuildFilterClause: {clauseStr}");
            return string.Join(" ", clauses);
        }

        // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Экранирование значений для TAG ---
        private static string EscapeTagValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = value;
            var specialChars = new[] { ',', '.', '<', '>', '{', '}', '[', ']', '"', '\'', ':', ';', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '~', '|', '/', '\\', '?', ' ' };

            foreach (var c in specialChars)
            {
                result = result.Replace(c.ToString(), "\\" + c);
            }

            return result;
        }

        // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Извлечение ID из ключа ---
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