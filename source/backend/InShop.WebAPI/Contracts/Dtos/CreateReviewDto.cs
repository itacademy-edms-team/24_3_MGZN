using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class CreateReviewDto
    {
        [Range(1, 5, ErrorMessage = "Рейтинг должен быть от 1 до 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Комментарий обязателен")]
        [MinLength(10, ErrorMessage = "Минимальная длина комментария - 10 символов")]
        [MaxLength(10000, ErrorMessage = "Максимальная длина комментария - 10000 символов")]
        public string Comment { get; set; } = null!;
    }
}
