using System.ComponentModel.DataAnnotations;

namespace Contracts.Dtos
{
    public class ProductUpdateDto
    {
        [Required]
        public string ProductName { get; set; } = null!;

        public string? ProductDescription { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal ProductPrice { get; set; }

        public bool ProductAvailability { get; set; }

        [Url]
        public string? ImageUrl { get; set; }
    }
}
