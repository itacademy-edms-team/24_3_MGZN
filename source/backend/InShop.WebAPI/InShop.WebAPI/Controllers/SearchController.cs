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
        private readonly IProductService _productService;

        // Пороги для косинусного расстояния
        private const double MainCosineDistanceThreshold = 0.5;
        private const double RecCosineDistanceThreshold = 0.99;

        // Веса для гибридного поиска
        private const double VectorWeight = 0.4;
        private const double LexicalWeight = 0.6;

        // Настройки блока рекомендаций
        private const int RecommendationLimit = 10;
        private const int RecommendationBuffer = 20;

        public SearchController(
            IEmbeddingService embeddingService,
            ConnectionMultiplexer redis,
            ILogger<SearchController> logger,
            IProductService productService)
        {
            _embeddingService = embeddingService;
            _redis = redis;
            _logger = logger;
            _productService = productService;
        }

        [HttpGet("specifications/filters")]
        public async Task<IActionResult> GetSpecificationFiltersForCategory([FromQuery] string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return BadRequest("Параметр 'categoryName' обязателен.");
            }

            var filtersDto = await _productService.GetSpecificationFiltersForCategoryAsync(categoryName);

            if (filtersDto == null)
            {
                return NotFound($"Фильтры для категории '{categoryName}' не найдены.");
            }

            return Ok(filtersDto);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchVectorPost(
            [FromBody] SearchRequestDto request,
            CancellationToken ct = default)
        {
            // --- 1. Валидация входных данных ---
            bool hasCategory = !string.IsNullOrWhiteSpace(request.Category);
            bool hasPriceFilter = request.MinPrice.HasValue || request.MaxPrice.HasValue;
            bool hasStockFilter = request.InStock.HasValue;
            bool hasSpecFilters = request.SpecFilters != null && request.SpecFilters.Any();
            bool hasQuery = !string.IsNullOrWhiteSpace(request.Query);

            if (!hasQuery && !hasCategory && !hasPriceFilter && !hasStockFilter && !hasSpecFilters)
            {
                return BadRequest("Необходимо указать хотя бы один параметр поиска.");
            }

            if (!IsValidSortParameter(request.SortBy, out string validatedSortBy))
            {
                return BadRequest($"Недопустимое значение параметра 'sortBy': '{request.SortBy}'.");
            }

            if (!IsValidSortOrder(request.SortOrder, out string validatedSortOrder))
            {
                return BadRequest($"Недопустимое значение параметра 'sortOrder': '{request.SortOrder}'.");
            }

            Dictionary<string, object>? validatedSpecFilters = null;
            if (request.SpecFilters != null && !string.IsNullOrEmpty(request.Category))
            {
                validatedSpecFilters = await _productService.ValidateSpecFiltersAsync(request.SpecFilters, request.Category);
                if (validatedSpecFilters == null)
                {
                    return BadRequest("Недопустимые фильтры по характеристикам.");
                }
            }
            else if (request.SpecFilters != null && string.IsNullOrEmpty(request.Category))
            {
                validatedSpecFilters = null;
            }

            try
            {
                // --- 2. Подготовка вектора и фильтров ---
                var queryVector = await _embeddingService.GenerateEmbeddingAsync(request.Query, ct);
                var queryVectorBytes = new byte[queryVector.Length * sizeof(float)];
                Buffer.BlockCopy(queryVector, 0, queryVectorBytes, 0, queryVectorBytes.Length);

                // 2.1. Фильтры для ОСНОВНОЙ выдачи (строгие: цена, категория, наличие, спеки)
                var mainFilterClause = BuildFilterClause(request.Category, request.MinPrice, request.MaxPrice, request.InStock, validatedSpecFilters);

                // 2.2. Фильтры для РЕКОМЕНДАЦИЙ
                // ПОЛЬЗОВАТЕЛЬСКОЕ ТРЕБОВАНИЕ: Только поисковой запрос, без фильтров.
                // Поэтому оставляем строку фильтра пустой.
                var recFilterClause = string.Empty;

                var escapedQueryTerms = string.Join(" ", request.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                int mainLimit = request.Limit > 0 ? request.Limit : 10;
                int recPoolLimit = mainLimit + RecommendationLimit + RecommendationBuffer;

                // Формируем векторные запросы
                // Основной: с фильтрами
                var mainVectorQuery = string.IsNullOrEmpty(mainFilterClause)
                    ? $"*=>[KNN {mainLimit} @embedding $BLOB AS vector_distance]"
                    : $"({mainFilterClause})=>[KNN {mainLimit} @embedding $BLOB AS vector_distance]";

                // Рекомендации: БЕЗ фильтров (только вектор запроса ко всем товарам)
                var recVectorQuery = $"*=>[KNN {recPoolLimit} @embedding $BLOB AS vector_distance]";

                // Лексические запросы
                // Основной: с фильтрами
                var mainLexicalQuery = string.IsNullOrEmpty(mainFilterClause)
                    ? escapedQueryTerms
                    : $"({mainFilterClause}) {escapedQueryTerms}";

                // Рекомендации: БЕЗ фильтров
                var recLexicalQuery = escapedQueryTerms;

                var db = _redis.GetDatabase();
                var batch = db.CreateBatch();

                // --- 3. Выполнение поиска через BATCH ---

                // Основная выдача
                var mainVectorTask = batch.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    mainVectorQuery,
                    "PARAMS", "2", "BLOB", (RedisValue)queryVectorBytes,
                    "RETURN", "8", "name", "description", "price", "category", "stock", "availability", "image_url", "vector_distance",
                    "DIALECT", "4"
                );

                var mainLexicalTask = batch.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    mainLexicalQuery,
                    "SCORER", "BM25",
                    "WITHSCORES",
                    "RETURN", "7", "name", "description", "price", "category", "stock", "availability", "image_url",
                    "LIMIT", "0", mainLimit.ToString(),
                    "DIALECT", "4"
                );

                // Рекомендации (расширенный пул, без фильтров)
                var recVectorTask = batch.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    recVectorQuery,
                    "PARAMS", "2", "BLOB", (RedisValue)queryVectorBytes,
                    "RETURN", "8", "name", "description", "price", "category", "stock", "availability", "image_url", "vector_distance",
                    "DIALECT", "4"
                );

                var recLexicalTask = batch.ExecuteAsync("FT.SEARCH",
                    "idx:products",
                    recLexicalQuery,
                    "SCORER", "BM25",
                    "WITHSCORES",
                    "RETURN", "7", "name", "description", "price", "category", "stock", "availability", "image_url",
                    "LIMIT", "0", recPoolLimit.ToString(),
                    "DIALECT", "4"
                );

                batch.Execute();
                await Task.WhenAll(mainVectorTask, mainLexicalTask, recVectorTask, recLexicalTask);

                // --- 4. Обработка результатов ---

                // 4.1. Основная выдача
                var mainHybridResults = await ProcessHybridSearch(
                    await mainVectorTask,
                    await mainLexicalTask,
                    MainCosineDistanceThreshold);

                var sortedMainResults = ApplySorting(mainHybridResults, validatedSortBy, validatedSortOrder);
                var finalMainDtos = sortedMainResults.Select(r => r.Dto).Take(mainLimit).ToList();
                var mainIds = new HashSet<int>(finalMainDtos.Select(p => p.Id));

                // 4.2. Рекомендации
                var recHybridResults = await ProcessHybridSearch(
                    await recVectorTask,
                    await recLexicalTask,
                    RecCosineDistanceThreshold);

                // Исключаем дубликаты из основной выдачи
                var uniqueRecCandidates = recHybridResults
                    .Where(r => !mainIds.Contains(r.Dto.Id))
                    .ToList();

                var sortedRecCandidates = ApplySorting(uniqueRecCandidates, validatedSortBy, validatedSortOrder);
                var finalRecDtos = sortedRecCandidates
                    .Select(r => r.Dto)
                    .Take(RecommendationLimit)
                    .ToList();

                var response = new SearchResponseDto
                {
                    Results = finalMainDtos,
                    Recommended = finalRecDtos
                };

                _logger.LogInformation("Поиск завершен. Основных: {MainCount}, Рекомендаций: {RecCount}",
                    response.Results.Count, response.Recommended.Count);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении гибридного поиска для запроса: '{Query}'", request.Query);
                return StatusCode(500, "Внутренняя ошибка сервера при выполнении поиска.");
            }
        }

        // --- МЕТОД: Построение фильтров для основного поиска (СТРОГИЕ) ---
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
                            string rangeClause = $"@{specName}:[";
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

            return string.Join(" ", clauses);
        }

        // --- ОСТАЛЬНЫЕ МЕТОДЫ (IsValidSortParameter, IsValidSortOrder, ApplySorting, ProcessHybridSearch, ParseRedisResultToDict, CreateDtoFromFields, EscapeTagValue, ExtractIdFromKey, HybridResult, ScoreLocation) ---

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

        private List<HybridResult> ApplySorting(List<HybridResult> results, string sortBy, string sortOrder)
        {
            if (!results.Any()) return results;

            IOrderedEnumerable<HybridResult> orderedResults = sortBy switch
            {
                "name" => sortOrder == "asc" ? results.OrderBy(r => r.Dto.Name, StringComparer.OrdinalIgnoreCase) : results.OrderByDescending(r => r.Dto.Name, StringComparer.OrdinalIgnoreCase),
                "price" => sortOrder == "asc" ? results.OrderBy(r => r.Dto.Price) : results.OrderByDescending(r => r.Dto.Price),
                _ => sortOrder == "asc" ? results.OrderBy(r => r.HybridScore) : results.OrderByDescending(r => r.HybridScore)
            };

            return orderedResults.ToList();
        }

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

            for (int i = 1; i < response.Length;)
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
                                }
                                else
                                {
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
                if (!dict.ContainsKey(product.Id))
                {
                    dict[product.Id] = (product, score.Value);
                }
            }

            return dict;
        }

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

        private static int ExtractIdFromKey(string key)
        {
            var lastColon = key.LastIndexOf(':');
            if (lastColon >= 0 && lastColon < key.Length - 1)
            {
                return int.Parse(key.Substring(lastColon + 1));
            }
            throw new ArgumentException($"Невозможно извлечь ID из ключа: {key}");
        }

        private class HybridResult
        {
            public ProductSearchResultDto Dto { get; set; } = null!;
            public double? VectorScore { get; set; }
            public double? LexicalScore { get; set; }
            public double HybridScore { get; set; }
        }

        public enum ScoreLocation { InArray, AfterKey }
    }
}