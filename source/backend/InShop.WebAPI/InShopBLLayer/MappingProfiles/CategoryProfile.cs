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
    internal class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            this.CreateMap<Category, CategoryDto>()
                .ForMember(d => d.ImageBase64, o => o.Ignore())
                .ForMember(d => d.RemoveImage, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.Products, o => o.Ignore());

            this.CreateMap<CategoryCreateDto, Category>()
                .ForMember(d => d.CategoryId, o => o.Ignore())
                .ForMember(d => d.ImageUrl, o => o.Ignore())
                .ForMember(d => d.Products, o => o.Ignore());
        }
    }
}
