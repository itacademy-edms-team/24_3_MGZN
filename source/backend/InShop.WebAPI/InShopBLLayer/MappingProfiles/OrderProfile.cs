﻿using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InShopDbModels.Models;
using Contracts.Dtos;

namespace InShopBLLayer.MappingProfiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            this.CreateMap<OrderDto, Order>().ReverseMap();
        }
    }
}
