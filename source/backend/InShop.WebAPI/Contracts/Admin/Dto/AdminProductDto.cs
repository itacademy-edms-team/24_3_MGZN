namespace Contracts.Admin.Dto
{
    public class AdminProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }
        public bool ProductAvailability { get; set; }
        public int ProductCategoryId { get; set; }
        public string? ProductCategoryName { get; set; }
        public int ProductStockQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public string? ImageUrl { get; set; }
    }
}
