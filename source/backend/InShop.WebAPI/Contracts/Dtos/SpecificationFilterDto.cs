using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class SpecificationFilterDto
    {
        [JsonPropertyName("specId")]
        public int SpecId { get; set; }

        [JsonPropertyName("name")] // Техническое имя (для Redis)
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("displayName")] // Отображаемое имя (для UI)
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("dataType")] // "Number" или "Text"
        public string DataType { get; set; } = string.Empty;

        // Опционально: Возможные значения для UI (например, для выпадающего списка)
        [JsonPropertyName("possibleValues")]
        public List<object>? PossibleValues { get; set; } = new List<object>();
    }
    // DTO для ответа на запрос списка фильтров для категории
    public class CategorySpecificationFiltersDto
    {
        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("filters")]
        public List<SpecificationFilterDto> Filters { get; set; } = new List<SpecificationFilterDto>();
    }
}
