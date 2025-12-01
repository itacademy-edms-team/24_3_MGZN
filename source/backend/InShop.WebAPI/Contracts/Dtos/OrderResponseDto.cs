using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public int ShipCompanyId { get; set; }
        public string ShipAddress { get; set; } = string.Empty;
        public string ShipMethod { get; set; } = string.Empty;
        public string PayStatus { get; set; } = string.Empty;
        public string PayMethod { get; set; } = string.Empty;
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;
        public decimal OrderTotalAmount { get; set; }
        public int SessionId { get; set; }

        public List<OrderItemResponse> OrderItems { get; set; } = new();
    }
}
