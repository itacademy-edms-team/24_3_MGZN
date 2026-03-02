using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using System.Text;
using System.Text.Json;

namespace InShop.WebAPI.Services
{
    public class HttpEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpEmbeddingService> _logger;

        public HttpEmbeddingService(HttpClient httpClient, ILogger<HttpEmbeddingService> logger)
        {
            // HttpClient внедряется через DI
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
        {
            _logger.LogDebug("Генерация вектора для текста: {Text}", text);

            var requestDto = new TextForEmbeddingDto { Text = $"{text};{text};{text};" };
            var jsonPayload = JsonSerializer.Serialize(requestDto);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                // Отправляем POST-запрос на наш FastAPI-сервер
                var response = await _httpClient.PostAsync("embed", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMsg = $"Сервер векторизации вернул ошибку {response.StatusCode}: {errorContent}";
                    _logger.LogError(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var resultDto = JsonSerializer.Deserialize<EmbeddingResponseDto>(responseContent);

                if (resultDto == null || resultDto.Embedding == null || resultDto.Embedding.Length == 0)
                {
                    var errorMsg = "Сервер векторизации вернул пустой или некорректный вектор.";
                    _logger.LogError(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                _logger.LogDebug("Вектор успешно сгенерирован, длина: {Length}", resultDto.Embedding.Length);
                return resultDto.Embedding;
            }
            catch (HttpRequestException httpEx)
            {
                var errorMsg = $"Ошибка HTTP при запросе к серверу векторизации: {httpEx.Message}";
                _logger.LogError(httpEx, errorMsg);
                throw new InvalidOperationException(errorMsg, httpEx);
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                // Операция была отменена
                _logger.LogDebug("Запрос на генерацию вектора отменён.");
                throw;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Ошибка при генерации вектора через HTTP: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                throw new InvalidOperationException(errorMsg, ex);
            }
        }
    }
}
