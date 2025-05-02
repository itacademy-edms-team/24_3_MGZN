using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos.OrderDtos
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = "Создан";
        public string OrderDate { get; set; }
        public string ShipMethod { get; set; } = null!;
        public string? ShipAddress { get; set; }
        public string? ShipDate { get; set; }
        public string PayStatus { get; set; } = null!;
        public string PayMethod { get; set; } = null!;
        public string CustomerFullName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string CustomerPhoneNumber { get; set; } = null!;
        public decimal OrderTotalAmount => OrderItems.Sum(item => item.TotalPrice);
        public int? ShipCompanyId { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
