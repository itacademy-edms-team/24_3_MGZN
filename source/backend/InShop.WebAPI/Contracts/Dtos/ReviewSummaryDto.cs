using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class ReviewSummaryDto
    {
        /// <summary>
        /// Список преимуществ, выделенных из отзывов.
        /// </summary>
        public List<string> Pros { get; set; } = new();

        /// <summary>
        /// Список недостатков, выделенных из отзывов.
        /// </summary>
        public List<string> Cons { get; set; } = new();

        /// <summary>
        /// Краткое итоговое резюме (2-3 предложения).
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Общее настроение отзывов.
        /// </summary>
        public string RatingTrend { get; set; } = "Neutral"; // Positive, Neutral, Negative
    }
}
