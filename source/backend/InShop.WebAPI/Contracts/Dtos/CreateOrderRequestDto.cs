using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class CreateOrderRequestDto
    {
        // ID сессии, по которому ищется заказ
        public int SessionId { get; set; }

        // Информация о доставке
        public int ShipCompanyId { get; set; }
        public string ShipAddress { get; set; } = string.Empty;
        public string ShipMethod { get; set; } = string.Empty;

        // Информация об оплате
        public string PayMethod { get; set; } = string.Empty;

        // Информация о клиенте
        public string CustomerFullname { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;

        // Состав заказа (новый или обновлённый)
        public List<CreateOrderItemRequest> OrderItems { get; set; } = new();
    }
}
