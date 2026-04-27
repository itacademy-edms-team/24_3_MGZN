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
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<SearchController> _logger;
        private readonly IProductService _productService;

        private const double MainCosineDistanceThreshold = 0.5;
        // Для рекомендаций порог мягче, чтобы найти больше похожих товаров
        private const double RecCosineDistanceThreshold = 0.8;

        private const double VectorWeight = 0.4;
        private const double LexicalWeight = 0.6;

        private const int RecommendationLimit = 10;
        private const int RecommendationBuffer = 20;

        // Максимальный лимит для одного запроса к Redis
        private const int MaxRedisFetchLimit = 200;

        public SearchController(
            IEmbeddingService embeddingService,
            IConnectionMultiplexer redis,
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
            if (request == null)
            {
                return BadRequest("Тело запроса отсутствует.");
            }

            var normalizedQuery = request.Query?.Trim() ?? string.Empty;

            // --- 1. Валидация ---
            bool hasCategory = !string.IsNullOrWhiteSpace(request.Category);
            bool hasPriceFilter = request.MinPrice.HasValue || request.MaxPrice.HasValue;
            bool hasStockFilter = request.InStock.HasValue;
            bool hasSpecFilters = request.SpecFilters != null && request.SpecFilters.Any();
            bool hasQuery = !string.IsNullOrWhiteSpace(normalizedQuery);

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

            // Параметры "пагинации"
            int limit = request.Limit > 0 ? request.Limit : 12;
            int offset = request.Offset > 0 ? request.Offset : 0;

            // Сколько всего товаров нам нужно получить из Redis для основной выдачи
            int totalNeeded = offset + limit;
            int fetchLimit = Math.Min(totalNeeded + 10, MaxRedisFetchLimit); // +10 буфер

            try
            {
                // --- 2. Подготовка вектора и фильтров ---

                // Если запроса нет, работаем только с фильтрами (без векторов)
                if (!hasQuery)
                {
                    var fallbackFilterClause = BuildFilterClause(request.Category, request.MinPrice, request.MaxPrice, request.InStock, validatedSpecFilters);
                    var filterOnlyQuery = string.IsNullOrEmpty(fallbackFilterClause) ? "*" : fallbackFilterClause;

                    var filterOnlyResult = await _redis.GetDatabase().ExecuteAsync("FT.SEARCH",
                        "idx:products",
                        filterOnlyQuery,
                        "RETURN", "7", "name", "description", "price", "category", "stock", "availability", "image_url",
                        "LIMIT", offset.ToString(), limit.ToString(),
                        "DIALECT", "4"
                    );

                    var fallbackProducts = ParseFlatSearchResults(filterOnlyResult);
                    return Ok(new SearchResponseDto
                    {
                        Results = fallbackProducts,
                        Recommended = new List<ProductSearchResultDto>() // Без query рекомендации не имеют смысла
                    });
                }

                var queryVector = await _embeddingService.GenerateEmbeddingAsync(normalizedQuery, ct);
                var queryVectorBytes = new byte[queryVector.Length * sizeof(float)];
                Buffer.BlockCopy(queryVector, 0, queryVectorBytes, 0, queryVectorBytes.Length);

                var escapedQueryTerms = string.Join(" ", normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                // 2.1. Фильтры для ОСНОВНОЙ выдачи (строгие)
                var mainFilterClause = BuildFilterClause(request.Category, request.MinPrice, request.MaxPrice, request.InStock, validatedSpecFilters);

                // 2.2. Фильтры для РЕКОМЕНДАЦИЙ (только запрос, без фильтров цены/наличия)
                // Мы хотим показать похожие товары, даже если они чуть дороже или в другой категории (если это уместно)
                // Но обычно категорию оставляют. Здесь сделаем полностью без фильтров, как вы просили ранее.
                var recFilterClause = string.Empty;

                // Формируем векторные запросы
                var mainVectorQuery = string.IsNullOrEmpty(mainFilterClause)
                    ? $"*=>[KNN {fetchLimit} @embedding $BLOB AS vector_distance]"
                    : $"({mainFilterClause})=>[KNN {fetchLimit} @embedding $BLOB AS vector_distance]";

                // Для рекомендаций берем с запасом
                var recVectorQuery = string.IsNullOrEmpty(recFilterClause)
                    ? $"*=>[KNN {RecommendationLimit + RecommendationBuffer} @embedding $BLOB AS vector_distance]"
                    : $"({recFilterClause})=>[KNN {RecommendationLimit + RecommendationBuffer} @embedding $BLOB AS vector_distance]";

                // Лексические запросы
                var mainLexicalQuery = string.IsNullOrEmpty(mainFilterClause)
                    ? escapedQueryTerms
                    : $"({mainFilterClause}) {escapedQueryTerms}";

                var recLexicalQuery = string.IsNullOrEmpty(recFilterClause)
                    ? escapedQueryTerms
                    : $"({recFilterClause}) {escapedQueryTerms}";

                var db = _redis.GetDatabase();
                var batch = db.CreateBatch();

                // --- 3. Выполнение поиска через BATCH ---

                // А. Основная выдача
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
                    "LIMIT", "0", fetchLimit.ToString(),
                    "DIALECT", "4"
                );

                // Б. Рекомендации (независимый поиск)
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
                    "LIMIT", "0", (RecommendationLimit + RecommendationBuffer).ToString(),
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

                // Пагинация в памяти
                var pagedMainResults = sortedMainResults
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                var finalMainDtos = pagedMainResults.Select(r => r.Dto).ToList();

                // Собираем IDs основной выдачи для исключения из рекомендаций
                var mainProductIds = new HashSet<int>(finalMainDtos.Select(p => p.Id));

                // 4.2. Рекомендации
                List<ProductSearchResultDto> finalRecDtos = new();

                // Вычисляем рекомендации только если это первая страница (offset == 0),
                // чтобы не тратить ресурсы впустую и не менять рекомендации при скролле
                if (offset == 0)
                {
                    var recHybridResults = await ProcessHybridSearch(
                        await recVectorTask,
                        await recLexicalTask,
                        RecCosineDistanceThreshold);

                    // Сортируем рекомендации
                    var sortedRecResults = ApplySorting(recHybridResults, validatedSortBy, validatedSortOrder);

                    // Исключаем дубликаты из основной выдачи
                    finalRecDtos = sortedRecResults
                        .Where(r => !mainProductIds.Contains(r.Dto.Id))
                        .Select(r => r.Dto)
                        .Take(RecommendationLimit)
                        .ToList();
                }

                var response = new SearchResponseDto
                {
                    Results = finalMainDtos,
                    Recommended = finalRecDtos
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении гибридного поиска для запроса: '{Query}'", request.Query);
                return StatusCode(500, "Внутренняя ошибка сервера при выполнении поиска.");
            }
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

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
                return dict;
            }

            var response = (RedisResult[])rawResult;
            if (response.Length < 1)
            {
                return dict;
            }

            for (int i = 1; i < response.Length;)
            {
                if (i >= response.Length || response[i].Resp2Type != ResultType.BulkString)
                {
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
                            i++;
                            continue;
                        }

                        var fieldsArray_InArray = (RedisResult[])response[i];
                        i++;

                        for (int j = 0; j < fieldsArray_InArray.Length; j += 2)
                        {
                            if (j + 1 >= fieldsArray_InArray.Length) break;

                            var fieldNameResult = fieldsArray_InArray[j];
                            var fieldValueResult = fieldsArray_InArray[j + 1];

                            if (fieldNameResult.Resp2Type != ResultType.BulkString || fieldValueResult.Resp2Type != ResultType.BulkString)
                            {
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
                            i++;
                            continue;
                        }

                        var possibleScoreValue_AfterKey = (string)response[i];
                        if (!double.TryParse(possibleScoreValue_AfterKey, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double extractedScore))
                        {
                            i++;
                            continue;
                        }
                        score = extractedScore;
                        i++;

                        if (i >= response.Length || response[i].Resp2Type != ResultType.Array)
                        {
                            i--;
                            continue;
                        }

                        var fieldsArray_AfterKey = (RedisResult[])response[i];
                        i++;

                        for (int j = 0; j < fieldsArray_AfterKey.Length; j += 2)
                        {
                            if (j + 1 >= fieldsArray_AfterKey.Length) break;

                            var fieldNameResult = fieldsArray_AfterKey[j];
                            var fieldValueResult = fieldsArray_AfterKey[j + 1];

                            if (fieldNameResult.Resp2Type != ResultType.BulkString || fieldValueResult.Resp2Type != ResultType.BulkString)
                            {
                                continue;
                            }

                            var fieldName = (string)fieldNameResult;
                            var fieldValue = (string)fieldValueResult;

                            fieldDict[fieldName] = fieldValue;
                        }
                        break;
                }

                if (!score.HasValue) continue;

                if (isVector && threshold.HasValue && score.Value > threshold.Value)
                {
                    continue;
                }

                var product = CreateDtoFromFields(key, fieldDict);
                if (product.Id <= 0)
                {
                    continue;
                }
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
                if (int.TryParse(key.Substring(lastColon + 1), out var id))
                {
                    return id;
                }
            }
            return -1;
        }

        private List<ProductSearchResultDto> ParseFlatSearchResults(RedisResult rawResult)
        {
            var items = new List<ProductSearchResultDto>();

            if (rawResult.Resp2Type != ResultType.Array)
            {
                return items;
            }

            var response = (RedisResult[])rawResult;
            for (int i = 1; i < response.Length;)
            {
                if (response[i].Resp2Type != ResultType.BulkString)
                {
                    break;
                }

                var key = (string)response[i];
                i++;

                if (i >= response.Length || response[i].Resp2Type != ResultType.Array)
                {
                    break;
                }

                var fieldsArray = (RedisResult[])response[i];
                i++;

                var fieldDict = new Dictionary<string, string>();
                for (int j = 0; j + 1 < fieldsArray.Length; j += 2)
                {
                    if (fieldsArray[j].Resp2Type != ResultType.BulkString || fieldsArray[j + 1].Resp2Type != ResultType.BulkString)
                    {
                        continue;
                    }

                    fieldDict[(string)fieldsArray[j]] = (string)fieldsArray[j + 1];
                }

                var dto = CreateDtoFromFields(key, fieldDict);
                if (dto.Id > 0)
                {
                    items.Add(dto);
                }
            }

            return items;
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