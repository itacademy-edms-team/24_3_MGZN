namespace InShopBLLayer.Models
{
    public class OrderEmailTemplateModel
    {
        public int OrderId { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string StatusDisplayName { get; set; } = string.Empty;
        public string OrderTotalAmount { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string TrackingUrl { get; set; } = string.Empty;
        public List<OrderItemTemplateModel> OrderItems { get; set; } = new();
    }
}
