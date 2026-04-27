using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class AiAnalysisService : IAiAnalysisService
    {
        private readonly IAiProvider _aiProvider;
        private readonly ILogger<AiAnalysisService> _logger;

        private const string SYSTEM_PROMPT = @"Ты — эксперт по анализу отзывов. Твоя задача — составить краткое резюме.
            Ответ должен быть СТРОГО в формате JSON. Никакого лишнего текста.
            Структура JSON:
            {
              ""pros"": [""Плюс 1"", ""Плюс 2""],
              ""cons"": [""Минус 1"", ""Минус 2""],
              ""summary"": ""Общий вывод 2-3 предложения."",
              ""rating_trend"": ""Positive"" | ""Neutral"" | ""Negative""
            }
            Язык ответа: Русский.";

        public AiAnalysisService(IAiProvider aiProvider, ILogger<AiAnalysisService> logger)
        {
            _aiProvider = aiProvider;
            _logger = logger;
        }

        public async Task<ReviewSummaryDto?> GenerateReviewSummaryAsync(List<string> reviewsText)
        {
            if (!reviewsText.Any()) return null;

            var selectedReviews = reviewsText.Take(50).ToList();
            var reviewsContent = string.Join("\n---\n", selectedReviews);
            var userPrompt = $"Отзывы о товаре:\n{reviewsContent}";

            try
            {
                var rawResponse = await _aiProvider.GenerateAsync(
                    SYSTEM_PROMPT,
                    userPrompt,
                    temperature: 0.2f,
                    maxTokens: 800
                );

                if (string.IsNullOrWhiteSpace(rawResponse))
                {
                    _logger.LogWarning("AI returned empty response");
                    return null;
                }

                var cleanJson = rawResponse
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var summary = JsonSerializer.Deserialize<ReviewSummaryDto>(cleanJson, options);

                if (summary?.Pros == null && summary?.Cons == null && string.IsNullOrEmpty(summary?.Summary))
                {
                    _logger.LogWarning("AI response parsed but fields are empty");
                    return null;
                }

                return summary;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI JSON response.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during review analysis");
                return null;
            }
        }
    }
}
