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
            CreateMap<(int SpecId, string Name, string DisplayName, string DataType, string? TextValue, decimal? NumberValue), ProductSpecDto>()
                .ForMember(dest => dest.SpecId, opt => opt.MapFrom(src => src.SpecId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.DataType))
                .ForMember(dest => dest.TextValue, opt => opt.MapFrom(src => src.TextValue))
                .ForMember(dest => dest.NumberValue, opt => opt.MapFrom(src => src.NumberValue));
        }
    }
}
