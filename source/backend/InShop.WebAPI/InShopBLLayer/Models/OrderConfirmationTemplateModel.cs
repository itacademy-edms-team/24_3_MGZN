using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Models
{
    public class OrderConfirmationTemplateModel
    {
        public int OrderId { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string OrderTotalAmount { get; set; } = string.Empty;
        public List<OrderItemTemplateModel> OrderItems { get; set; } = new();
    }
}
