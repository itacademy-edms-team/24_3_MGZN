using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class SearchRequestDto
    {
        [JsonPropertyName("q")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;
        // Смещение (сколько товаров уже загружено)
        public int Offset { get; set; } = 0;

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("minPrice")]
        public decimal? MinPrice { get; set; }

        [JsonPropertyName("maxPrice")]
        public decimal? MaxPrice { get; set; }

        [JsonPropertyName("inStock")]
        public bool? InStock { get; set; }

        [JsonPropertyName("sortBy")]
        public string SortBy { get; set; } = "relevance";

        [JsonPropertyName("sortOrder")]
        public string SortOrder { get; set; } = "desc";

        [JsonPropertyName("specFilters")]
        public Dictionary<string, object>? SpecFilters { get; set; }
    }
}
