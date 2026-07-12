using InShopDbModels.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Repositories
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly AppDbContext _appDbContext;
        public OrderItemRepository(AppDbContext context)
        {
            _appDbContext = context;
        }

        public async Task<bool> CheckVerifiedPurchaseAsync(int sessionId, int productId)
        {
            // В БД пишется канонический Delivered; legacy-алиасы — на случай старых заказов.
            // Paid/Shipped не считаем: «проверенный покупатель» = товар получен.
            var deliveredStatuses = new[] { "Delivered", "Доставлен", "Завершен" };

            return await _appDbContext.OrderItems
                .AnyAsync(oi => oi.Order.SessionId == sessionId
                             && oi.ProductId == productId
                             && deliveredStatuses.Contains(oi.Order.OrderStatus));
        }
    }
}

