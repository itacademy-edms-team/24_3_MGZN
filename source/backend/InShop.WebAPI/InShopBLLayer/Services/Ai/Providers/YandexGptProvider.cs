using Contracts.Optoins;
using InShopBLLayer.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InShopBLLayer.Services.Ai.Providers
{
    public class YandexGptProvider : IAiProvider
    {
        private const string YANDEX_ENDPOINT = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

        private readonly HttpClient _httpClient;
        private readonly AiProviderSettings _settings;
        private readonly ILogger<YandexGptProvider> _logger;
        private readonly string _modelUri;

        public YandexGptProvider(
            HttpClient httpClient,
            AiProviderSettings settings,
            ILogger<YandexGptProvider> logger)
        {
            _httpClient = httpClient;
            _settings = settings;
            _logger = logger;

            if (string.IsNullOrEmpty(settings.FolderId))
                throw new InvalidOperationException("FolderId is required for YandexGPT provider.");

            _modelUri = $"gpt://{settings.FolderId}/{settings.ModelName}/latest";
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Api-Key", _settings.ApiKey);
        }

        public async Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            float temperature = 0.2f,
            int maxTokens = 1000)
        {
            var requestBody = new
            {
                modelUri = _modelUri,
                completionOptions = new
                {
                    stream = false,
                    temperature = temperature,
                    maxTokens = maxTokens
                },
                messages = new[]
                {
                    new { role = "system", text = systemPrompt },
                    new { role = "user", text = userPrompt }
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                _logger.LogDebug("Calling YandexGPT: {ModelUri}", _modelUri);

                var response = await _httpClient.PostAsync(YANDEX_ENDPOINT, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("YandexGPT API error: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    throw new InvalidOperationException($"YandexGPT request failed: {response.StatusCode}");
                }

                using var doc = JsonDocument.Parse(responseContent);
                var content = doc.RootElement
                    .GetProperty("result")
                    .GetProperty("alternatives")[0]
                    .GetProperty("message")
                    .GetProperty("text")
                    .GetString();

                return content ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during YandexGPT generation");
                throw;
            }
        }
    }
}
