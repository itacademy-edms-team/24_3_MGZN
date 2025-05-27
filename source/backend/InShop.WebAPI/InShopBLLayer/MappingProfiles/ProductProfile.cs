using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Dtos;
using InShopDbModels.Models;

namespace InShopBLLayer.MappingProfiles
{
    internal class ProductProfile: Profile
    {
        public ProductProfile()
        {
            this.CreateMap<Product, ProductDto>()
                    .ForMember(p => p.ProductCategoryName, o => o.MapFrom(t => t.ProductCategory.CategoryName))
                    .ReverseMap();
            this.CreateMap<ProductCreateDto, Product>();
        }
    }
}
