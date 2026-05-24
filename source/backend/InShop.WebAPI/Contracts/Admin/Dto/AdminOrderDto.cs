namespace Contracts.Admin.Dto
{
    public class AdminOrderDto
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = null!;
        /// <summary>Сырой статус в БД (для отладки legacy).</summary>
        public string? RawOrderStatus { get; set; }
        public DateOnly OrderDate { get; set; }
        public string CustomerFullname { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string CustomerPhoneNumber { get; set; } = null!;
        public decimal OrderTotalAmount { get; set; }
        public string PayStatus { get; set; } = null!;
        public int ItemsCount { get; set; }
    }
}
