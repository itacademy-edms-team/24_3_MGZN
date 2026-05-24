using AutoMapper;
using Contracts.Admin.Dto;
using InShopDbModels.Models;

namespace InShopBLLayer.MappingProfiles
{
    internal class AdminProfile : Profile
    {
        public AdminProfile()
        {
            CreateMap<Product, AdminProductDto>()
                .ForMember(d => d.ProductCategoryName, o => o.MapFrom(s => s.ProductCategory.CategoryName));

            CreateMap<AdminProductCreateDto, Product>()
                .ForMember(d => d.ProductId, o => o.Ignore())
                .ForMember(d => d.ImageUrl, o => o.Ignore())
                .ForMember(d => d.ReservedQuantity, o => o.Ignore())
                .ForMember(d => d.RowVersion, o => o.Ignore())
                .ForMember(d => d.AverageRating, o => o.Ignore())
                .ForMember(d => d.ReviewsCount, o => o.Ignore());

            CreateMap<AdminProductUpdateDto, Product>()
                .ForMember(d => d.ProductId, o => o.Ignore())
                .ForMember(d => d.ImageUrl, o => o.Ignore())
                .ForMember(d => d.ReservedQuantity, o => o.Ignore())
                .ForMember(d => d.RowVersion, o => o.Ignore())
                .ForMember(d => d.AverageRating, o => o.Ignore())
                .ForMember(d => d.ReviewsCount, o => o.Ignore());

            CreateMap<Order, AdminOrderDto>()
                .ForMember(d => d.ItemsCount, o => o.Ignore())
                .ForMember(d => d.RawOrderStatus, o => o.Ignore())
                .ForMember(d => d.OrderStatus, o => o.MapFrom(s => s.OrderStatus));
        }
    }
}
