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
            this.CreateMap<Category, CategoryDto>().ReverseMap();
            this.CreateMap<CategoryCreateDto, Category>();
        }
    }
}
