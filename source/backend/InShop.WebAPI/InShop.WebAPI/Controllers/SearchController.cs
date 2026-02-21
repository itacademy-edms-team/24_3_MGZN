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

        public SearchController(IEmbeddingService embeddingService, ConnectionMultiplexer redis, ILogger<SearchController> logger)
        {
            _embeddingService = embeddingService;
            _redis = redis;
            _logger = logger;
        }

        [HttpGet("vector-search")]
        public async Task<IActionResult> SearchVector(
            [FromQuery] string q,
            [FromQuery] int limit = 20,
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
                var query = $"{baseQuery}=>[KNN {limit} @embedding $BLOB AS vector_score]";

                _logger.LogDebug("Формируемая строка запроса для FT.SEARCH: '{Query}'", query);

                var db = _redis.GetDatabase();

                // ИСПРАВЛЕНО: используем правильный формат параметров
                var result = await db.ExecuteAsync("FT.SEARCH",
                    "idx:products",           // индекс
                    query,                    // запрос с KNN
                    "PARAMS", "2", "BLOB", queryVectorBytes,  // параметры
                    "RETURN", "7", "name", "description", "price", "category", "stock", "availability", "image_url", // поля для возврата
                    "SORTBY", "vector_score",  // сортировка по векторной оценке
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

            if (rawResult.Resp2Type != ResultType.Array)
            {
                _logger.LogError("Ожидался Array результат от FT.SEARCH, получен: {ResultType}", rawResult.Resp2Type);
                return StatusCode(500, "Внутренняя ошибка сервера: неверный формат результата от поиска.");
            }

            var response = (RedisResult[])rawResult;

            //if (response.Length < 2)
            //{
            //    _logger.LogError("Неверный формат ответа FT.SEARCH: недостаточно элементов. Length: {Length}", response.Length);
            //    return StatusCode(500, "Внутренняя ошибка сервера: неверный формат результата поиска.");
            //}

            var totalResults = (long)response[0];
            _logger.LogDebug("Всего результатов по запросу: {TotalResults}", totalResults);

            var products = new List<ProductSearchResultDto>();

            // ИСПРАВЛЕНО: правильная обработка пар ключ-значение
            for (int i = 1; i < response.Length; i++)
            {
                var key = (string)response[i];
                i++;

                if (i >= response.Length) break;

                var fields = (RedisResult[])response[i];

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
                for (int j = 0; j < fields.Length; j += 2)
                {
                    if (j + 1 >= fields.Length) break;

                    var fieldName = fields[j].ToString();
                    var fieldValue = fields[j + 1].ToString();

                    if (fieldName != null && fieldValue != null)
                    {
                        fieldDict[fieldName] = fieldValue;
                    }
                }

                return new ProductSearchResultDto
                {
                    Id = ExtractIdFromKey(key),
                    Name = fieldDict.GetValueOrDefault("name", string.Empty),
                    Description = fieldDict.GetValueOrDefault("description", string.Empty),
                    Price = decimal.TryParse(fieldDict.GetValueOrDefault("price"), out var price) ? price : 0,
                    Category = fieldDict.GetValueOrDefault("category", string.Empty),
                    StockQuantity = int.TryParse(fieldDict.GetValueOrDefault("stock"), out var stock) ? stock : 0,
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