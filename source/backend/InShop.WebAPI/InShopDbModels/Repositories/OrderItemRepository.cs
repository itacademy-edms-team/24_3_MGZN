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
    //public class OrderItemRepository : IOrderItemRepository
    //{
    //    private readonly AppDbContext _appDbContext;
    //    public OrderItemRepository(AppDbContext context)
    //    {
    //        _appDbContext = context;
    //    }

    //    public async Task<int> AddItem(OrderItem item)
    //    {
    //        await _appDbContext.OrderItems.AddAsync(item);
    //        await _appDbContext.SaveChangesAsync();
    //        return item.OrderItemId;
    //    }

    //    public async Task DeleteAllItemsByOrderId(int orderId)
    //    {
    //        _appDbContext.OrderItems.Remove(oi => oi.OrderId == orderId);
    //        await _appDbContext.SaveChangesAsync();
    //    }

    //    public async Task DeleteItem(OrderItem item)
    //    {
    //        _appDbContext.OrderItems.Remove(item);
    //        await _appDbContext.SaveChangesAsync();
    //    }

    //    public async Task<IEnumerable<OrderItem>> GetAllItemsByOrderId(int orderId)
    //    {
    //        return await _appDbContext.OrderItems.Where(oi => oi.OrderId == orderId).ToListAsync();
    //    }

    //    public async Task<Product> GetProductByItem(OrderItem item)
    //    {
    //        var productId = item.ProductId;
    //        return await _appDbContext.Products.Where(p => p.ProductId == productId).FirstOrDefaultAsync();
    //    }
    //}
}

