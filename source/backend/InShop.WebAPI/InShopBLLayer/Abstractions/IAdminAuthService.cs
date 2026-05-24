using Contracts.Admin.Dto;
using System.Security.Claims;

namespace InShopBLLayer.Abstractions
{
    public interface IAdminAuthService
    {
        Task RegisterFirstAdminAsync(AdminRegisterDto dto, CancellationToken ct = default);
        Task<AdminAuthResponseDto> LoginAsync(AdminLoginDto dto, CancellationToken ct = default);
        AdminMeDto GetMe(ClaimsPrincipal user);
    }
}
