using System.ComponentModel.DataAnnotations;

namespace Contracts.Dtos.ProductsDtos
{
    public class ProductCreateDto
    {
        [Required(ErrorMessage = "Название товара обязательно")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 100 символов")]
        public string ProductName { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string? ProductDescription { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal ProductPrice { get; set; }

        [Required]
        public int ProductCategoryId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
        public int ProductStockQuantity { get; set; }

        [Url(ErrorMessage = "Некорректный URL изображения")]
        public string? ImageUrl { get; set; }
    }
}
