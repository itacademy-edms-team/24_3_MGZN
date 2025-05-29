using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 50 символов")]
        public string CategoryName { get; set; } = null!;
    }
}
