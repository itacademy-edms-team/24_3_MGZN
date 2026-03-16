using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class ProductSpecDto
    {
        public int SpecId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }

        public string? TextValue { get; set; }
        public decimal? NumberValue { get; set; }
    }
}
