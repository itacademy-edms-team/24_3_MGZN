using AutoMapper;
using Contracts.Dtos.OrderDtos;
using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.MappingProfiles
{
    internal class OrderProfile : Profile
    {
        public OrderProfile()
        {
            this.CreateMap<Order, OrderDto>().ReverseMap();
            this.CreateMap<OrderCreateDto, Order>();
        }
    }
}
