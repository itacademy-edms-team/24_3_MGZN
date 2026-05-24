namespace InShopBLLayer.Abstractions
{
    /// <summary>
    /// Управление резервированием складских остатков товара с оптимистичной блокировкой.
    /// </summary>
    public interface IInventoryReservationService
    {
        /// <summary>
        /// Переводит единицы из свободного остатка (ProductStockQuantity) в резерв (ReservedQuantity).
        /// Физический остаток на складе не меняется.
        /// </summary>
        Task ReserveAsync(int productId, int quantity, CancellationToken ct = default);

        /// <summary>
        /// Откатывает резерв (например, при отмене заказа): ReservedQuantity → ProductStockQuantity.
        /// </summary>
        Task ReleaseAsync(int productId, int quantity, CancellationToken ct = default);

        /// <summary>
        /// Окончательное списание из резерва после подтверждения оплаты/отгрузки.
        /// Уменьшает только ReservedQuantity; физический остаток уменьшается на quantity.
        /// </summary>
        Task FinalizeAsync(int productId, int quantity, CancellationToken ct = default);
    }
}
