using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class ReviewResponseDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int SessionId { get; set; } // ID сессии автора
        public int Rating { get; set; }
        public string Comment { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Информация о полезности
        public int VoteScore { get; set; } // Сумма голосов (Up - Down)
        public int? UserVote { get; set; } // Голос текущего пользователя (1, -1 или null)

        // Флаг, покупал ли пользователь этот товар
        public bool IsVerifiedPurchase { get; set; }

        // Является ли текущий авторизованный пользователь владельцем отзыва
        public bool IsOwner { get; set; }
    }
}
