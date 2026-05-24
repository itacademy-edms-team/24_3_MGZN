using Contracts.Admin.Dto;

namespace InShopBLLayer.Abstractions
{
    public interface IAdminOrderService
    {
        Task<PagedResultDto<AdminOrderDto>> GetOrdersAsync(
            int page,
            int pageSize,
            string? statusFilter,
            CancellationToken ct = default);

        Task<PagedResultDto<AdminOrderDto>> GetDraftOrdersAsync(int page, int pageSize, CancellationToken ct = default);

        Task<AdminOrderDto?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);

        Task<AdminOrderDetailDto?> GetOrderDetailsAsync(int orderId, CancellationToken ct = default);

        Task<AdminOrderDto> ChangeOrderStatusAsync(
            int orderId,
            string newStatus,
            string adminEmail,
            CancellationToken ct = default);

        IReadOnlyList<string> GetAllowedNextStatuses(string? currentStatus);
    }
}
