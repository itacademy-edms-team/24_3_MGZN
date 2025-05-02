using AutoMapper;
using Contracts.Dtos.OrderDtos;
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
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;
        public OrderService(IOrderRepository orderRepository, IMapper mapper)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
        }
        public async Task<IEnumerable<OrderDto>> GetOrders()
        {
            var orders = await _orderRepository.GetOrders();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        public async Task<OrderDto> GetOrder(int id)
        {
            var order = _orderRepository.GetOrder(id);
            return order == null ? null : _mapper.Map<OrderDto>(order);
        }

        Task<IEnumerable<Order>> IOrderService.GetOrders()
        {
            throw new NotImplementedException();
        }

        Task<Order> IOrderService.GetOrder(int id)
        {
            throw new NotImplementedException();
        }

        public Task CreateOrder(Order newOrder)
        {
            throw new NotImplementedException();
        }

        public Task DeleteOrder(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }
    }
}
