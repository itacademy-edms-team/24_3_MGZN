using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class TextForEmbeddingDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
