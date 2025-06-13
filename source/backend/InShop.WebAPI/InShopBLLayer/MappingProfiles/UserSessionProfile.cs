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
            this.CreateMap<UserSessionDto, UserSession>();
        }
    }
}
