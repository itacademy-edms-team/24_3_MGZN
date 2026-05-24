namespace Contracts.Admin.Dto
{
    public class AdminOrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = null!;
        public string? RawOrderStatus { get; set; }
        public DateOnly OrderDate { get; set; }
        public decimal OrderTotalAmount { get; set; }
        public string PayStatus { get; set; } = null!;
        public string PayMethod { get; set; } = null!;
        public string CustomerFullname { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string CustomerPhoneNumber { get; set; } = null!;
        public int SessionId { get; set; }
        public string? ShipAddress { get; set; }
        public DateOnly? ShipDate { get; set; }
        public string ShipMethod { get; set; } = null!;
        public string? ShipCompanyName { get; set; }
        public List<AdminOrderItemDetailDto> Items { get; set; } = new();
        public List<AdminOrderAuditEntryDto> StatusHistory { get; set; } = new();
    }

    public class AdminOrderItemDetailDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class AdminOrderAuditEntryDto
    {
        public DateTime CreatedAt { get; set; }
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = null!;
        public string ChangedBy { get; set; } = null!;
    }
}
