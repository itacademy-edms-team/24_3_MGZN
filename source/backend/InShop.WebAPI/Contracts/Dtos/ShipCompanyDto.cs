using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class ShipCompanyDto
    {
        public int ShipCompanyId { get; set; }

        public string ShipCompanyName { get; set; } = null!;

        public string Contact { get; set; } = null!;
    }
}
