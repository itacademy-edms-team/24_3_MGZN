using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Optoins
{
    public class AiProviderSettings
    {
        /// <summary>
        /// Тип провайдера: "Yandex", "OpenAI", "DeepSeek", "Ollama".
        /// </summary>
        public string ProviderType { get; set; } = "Yandex";

        /// <summary>
        /// API Key или Токен доступа.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// ID каталога (обязательно для Yandex).
        /// </summary>
        public string? FolderId { get; set; }

        /// <summary>
        /// Базовый URL (для OpenAI-совместимых API, например DeepSeek или локальный Ollama).
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Название модели (например, "yandexgpt-lite", "gpt-4o-mini").
        /// </summary>
        public string ModelName { get; set; } = "yandexgpt-lite";

        /// <summary>
        /// Таймаут запроса в секундах.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;
    }
}
