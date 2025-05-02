using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InShopDbModels.Models;
using Contracts.Dtos.ProductsDtos;

namespace InShopBLLayer.MappingProfiles
{
    internal class ProductProfile: Profile
    {
        public ProductProfile()
        {
            this.CreateMap<Product, ProductDto>().ReverseMap();
            this.CreateMap<ProductCreateDto, Product>();
        }
    }
}
