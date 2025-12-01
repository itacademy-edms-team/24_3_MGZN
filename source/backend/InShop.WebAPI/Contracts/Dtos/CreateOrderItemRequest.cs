using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class CreateOrderItemRequest
    {
        public int ProductId { get; set; }
        public int QuantityItem { get; set; }
        public decimal Price { get; set; }
    }
}
