using System.ComponentModel.DataAnnotations;

namespace Contracts.Admin.Dto
{
    public class AdminProductUpdateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string ProductName { get; set; } = null!;

        [StringLength(2000)]
        public string? ProductDescription { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal ProductPrice { get; set; }

        public bool ProductAvailability { get; set; }

        [Required]
        public int ProductCategoryId { get; set; }

        [Range(0, int.MaxValue)]
        public int ProductStockQuantity { get; set; }

        public string? ImageBase64 { get; set; }
        public string? ImageUrl { get; set; }

        /// <summary>Удалить привязку изображения (очистить ImageUrl в БД).</summary>
        public bool RemoveImage { get; set; }
    }
}
