using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Models
{
    public class PaymentStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public string? Error {  get; set; }
    }
}
