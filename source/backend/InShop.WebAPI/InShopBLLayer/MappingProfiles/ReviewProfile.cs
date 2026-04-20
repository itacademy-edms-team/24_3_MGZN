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
    public class ReviewProfile : Profile
    {
        public ReviewProfile()
        {
            // Маппинг из сущности БД в DTO для ответа
            this.CreateMap<ProductReview, ReviewResponseDto>()
                .ForMember(dest => dest.VoteScore, opt => opt.MapFrom(src => src.ReviewVotes.Sum(v => v.VoteType)))
                .ForMember(dest => dest.UserVote, opt => opt.Ignore()) // UserVote зависит от текущей сессии, маппим вручную или через AfterMap
                .ForMember(dest => dest.IsVerifiedPurchase, opt => opt.Ignore()); // Тоже вычисляется отдельно

            // Маппинг CreateDto в сущность (для удобства создания)
            this.CreateMap<CreateReviewDto, ProductReview>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.ProductId, opt => opt.Ignore()) // Заполняется в сервисе
                .ForMember(dest => dest.SessionId, opt => opt.Ignore()); // Заполняется в сервисе
            this.CreateMap<UpdateReviewDto, ProductReview>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.ProductId, opt => opt.Ignore()) // Не меняем ID товара
            .ForMember(dest => dest.SessionId, opt => opt.Ignore()) // Не меняем автора
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()); // Не меняем дату создания
        }
    }
}
