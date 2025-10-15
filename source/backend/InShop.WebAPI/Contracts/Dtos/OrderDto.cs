using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class OrderDto
    {
        public string OrderStatus { get; set; } = null!;

        public DateOnly OrderDate { get; set; }

        public string ShipMethod { get; set; } = null!;

        public string PayStatus { get; set; } = null!;

        public string CustomerFullname { get; set; } = null!;

        public string PayMethod { get; set; } = null!;

        public string CustomerEmail { get; set; } = null!;

        public string CustomerPhoneNumber { get; set; } = null!;

        public int SessionId { get; set; }

    }
}
