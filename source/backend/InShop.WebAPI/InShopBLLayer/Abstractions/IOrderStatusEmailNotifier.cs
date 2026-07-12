using InShopDbModels.Models;

namespace InShopBLLayer.Abstractions
{
    public interface IOrderStatusEmailNotifier
    {
        Task SendOrderConfirmationAsync(Order order, CancellationToken ct = default);

        /// <summary>Письмо при Paid / Shipped / Delivered / Cancelled. Остальные статусы — no-op.</summary>
        Task SendStatusUpdateAsync(Order order, CancellationToken ct = default);
    }
}
