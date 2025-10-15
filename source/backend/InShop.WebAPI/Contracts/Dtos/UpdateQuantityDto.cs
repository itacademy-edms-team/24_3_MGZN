using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class UpdateQuantityDto
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
    }
}
