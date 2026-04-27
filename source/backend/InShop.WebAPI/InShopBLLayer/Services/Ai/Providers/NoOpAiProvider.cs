using InShopBLLayer.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services.Ai.Providers
{
    public class NoOpAiProvider : IAiProvider
    {
        private readonly ILogger<NoOpAiProvider>? _logger;

        public NoOpAiProvider(ILogger<NoOpAiProvider>? logger = null)
        {
            _logger = logger;
        }

        public Task<string> GenerateAsync(string systemPrompt, string userPrompt, float temperature = 0.2f, int maxTokens = 1000)
        {
            _logger?.LogWarning("NoOpAiProvider called - returning mock response.");

            return Task.FromResult(@"{
                ""pros"": [""Хорошее качество"", ""Быстрая доставка""],
                ""cons"": [""Упаковка могла быть лучше""],
                ""summary"": ""Покупатели довольны качеством, но есть замечания к упаковке."",
                ""rating_trend"": ""Positive""
            }");
        }
    }
}
