namespace Contracts.Dtos
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }
        public bool ProductAvailability { get; set; }
        public int ProductStockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductCategoryId { get; set; }
        public string ProductCategoryName { get; set; } = null!;
        
    }
}
