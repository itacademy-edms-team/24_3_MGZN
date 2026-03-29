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
    public class UserSessionProfile : Profile
    {
        public UserSessionProfile()
        {
            // ═══════════════════════════════════════════════════════
            // DTO → Entity (для создания/обновления)
            // ═══════════════════════════════════════════════════════

            CreateMap<UserSessionDto, UserSession>()
                .ForMember(dest => dest.UserIpaddress, opt => opt.MapFrom(src => src.UserIpaddress))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                // Системные поля игнорируем - устанавливаются в сервисе
                .ForMember(dest => dest.SessionId, opt => opt.Ignore())
                .ForMember(dest => dest.SessionToken, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            // ═══════════════════════════════════════════════════════
            // Entity → DTO (для чтения/возврата)
            // ═══════════════════════════════════════════════════════

            CreateMap<UserSession, SessionCreationResult>()
                .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.SessionId))
                .ForMember(dest => dest.OrderId, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt!.Value))
                .ForMember(dest => dest.Message, opt => opt.Ignore());

            CreateMap<UserSession, SessionValidationResult>()
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(
                    src => src.IsActive && src.ExpiresAt > DateTime.UtcNow))
                .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.SessionId))
                .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt))
                .ForMember(dest => dest.Message, opt => opt.Ignore());
        }
    }
}
