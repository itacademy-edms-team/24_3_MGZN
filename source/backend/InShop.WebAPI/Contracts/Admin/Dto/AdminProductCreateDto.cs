using System.ComponentModel.DataAnnotations;

namespace Contracts.Admin.Dto
{
    public class AdminProductCreateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string ProductName { get; set; } = null!;

        [StringLength(2000)]
        public string? ProductDescription { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal ProductPrice { get; set; }

        public bool ProductAvailability { get; set; } = true;

        [Required]
        public int ProductCategoryId { get; set; }

        [Range(0, int.MaxValue)]
        public int ProductStockQuantity { get; set; }

        /// <summary>data:image/png;base64,... или чистый Base64.</summary>
        public string? ImageBase64 { get; set; }

        public string? ImageUrl { get; set; }
    }
}
