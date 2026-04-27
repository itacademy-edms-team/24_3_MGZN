using Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IReviewService
    {
        // Получить отзывы с пагинацией и сортировкой по полезности
        Task<(List<ReviewResponseDto> Reviews, int TotalCount)> GetProductReviewsAsync(int productId, int page, int pageSize, int? currentSessionId);
        Task<int> GetReviewCountAsync(int productId);

        // Создать отзыв
        Task<ReviewResponseDto> AddReviewAsync(int productId, int sessionId, CreateReviewDto dto);

        // Обновить отзыв
        Task<ReviewResponseDto> UpdateReviewAsync(int reviewId, int sessionId, UpdateReviewDto dto);

        // Удалить отзыв
        Task DeleteReviewAsync(int reviewId, int sessionId);

        // Проголосовать за полезность
        Task VoteReviewAsync(int reviewId, int sessionId, int voteType);

        /// Получает список текстов отзывов для AI-анализа.
        /// Возвращает только последние N отзывов, отсортированных по дате.
        Task<List<string>> GetRecentReviewTextsAsync(int productId, int count = 50);
    }
}
