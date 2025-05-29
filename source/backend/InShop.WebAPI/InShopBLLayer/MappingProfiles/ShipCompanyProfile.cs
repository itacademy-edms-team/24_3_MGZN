using AutoMapper;
using Contracts.Dtos;
using InShopDbModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.MappingProfiles
{
    public class ShipCompanyProfile : Profile
    {
        public ShipCompanyProfile()
        {
            this.CreateMap<ShipCompany, ShipCompanyDto>().ReverseMap();
            this.CreateMap<ShipCompanyCreateDto, ShipCompany>();
        }
    }
}
