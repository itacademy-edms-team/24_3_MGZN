namespace Contracts.Dtos;

/// <summary>Публичные данные заказа для страницы отслеживания (без session/audit).</summary>
public class OrderTrackDto
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string OrderStatusDisplay { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public decimal OrderTotalAmount { get; set; }
    public string ShipMethod { get; set; } = string.Empty;
    public string? ShipAddress { get; set; }
    public string CustomerFullname { get; set; } = string.Empty;
    public string PayMethod { get; set; } = string.Empty;
    public string PayStatus { get; set; } = string.Empty;
    public List<OrderTrackItemDto> Items { get; set; } = new();
}

public class OrderTrackItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
