using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class SearchResponseDto
    {
        public List<ProductSearchResultDto> Results { get; set; } = new();
        public List<ProductSearchResultDto> Recommended { get; set; } = new();
    }
}
