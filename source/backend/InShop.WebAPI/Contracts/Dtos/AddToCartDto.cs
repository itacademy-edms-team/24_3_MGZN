using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int SessionId { get; set; }
    }
}
