using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Models
{
    public class OrderItemTemplateModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int QuantityItem { get; set; }
        public string Price { get; set; } = string.Empty;
        public string TotalPrice { get; set; } = string.Empty;
    }
}

