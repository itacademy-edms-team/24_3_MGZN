using AutoMapper;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    //public class OrderItemService : IOrderItemService
    //{
    //    private readonly IOrderItemRepository _orderItemRepository;
    //    private readonly IMapper _mapper;
    //    public OrderItemService(IOrderItemRepository orderItemRepository, IMapper mapper)
    //    {
    //        _orderItemRepository = orderItemRepository;
    //        _mapper = mapper; 
    //    }

    //    public Task<int> AddItem(OrderItemDto itemDto)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task DeleteAllItemsByOrderId(int orderId)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task DeleteItem(OrderItemDto itemDto)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<IEnumerable<OrderItem>> GetAllItemsByOrderId(int orderId)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<Product> GetProductByItem(OrderItem item)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
