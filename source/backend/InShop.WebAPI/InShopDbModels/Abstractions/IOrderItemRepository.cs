using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopDbModels.Abstractions
{
    public interface IOrderItemRepository
    {
        Task<bool> CheckVerifiedPurchaseAsync(int sessionId, int productId);

    }
}
