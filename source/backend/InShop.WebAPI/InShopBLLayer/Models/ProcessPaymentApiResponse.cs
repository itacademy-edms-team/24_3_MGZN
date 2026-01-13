using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Models
{
    public class ProcessPaymentApiResponse
    {
        public string Status { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string? Message {  get; set; }
    }
}
