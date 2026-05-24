using Contracts.Admin.Dto;

namespace InShopBLLayer.Abstractions
{
    public interface IAdminProductService
    {
        Task<PagedResultDto<AdminProductDto>> GetProductsAsync(int page, int pageSize, CancellationToken ct = default);
        Task<AdminProductDto?> GetProductAsync(int id, CancellationToken ct = default);
        Task<AdminProductDto> CreateProductAsync(AdminProductCreateDto dto, CancellationToken ct = default);
        Task<AdminProductDto> UpdateProductAsync(int id, AdminProductUpdateDto dto, CancellationToken ct = default);
        Task DeleteProductAsync(int id, CancellationToken ct = default);
    }
}
