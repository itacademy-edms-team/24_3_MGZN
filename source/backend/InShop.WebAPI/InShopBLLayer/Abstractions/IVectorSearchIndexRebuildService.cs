namespace InShopBLLayer.Abstractions
{
    /// <summary>
    /// Полная перестройка поискового индекса Redis (вектор + характеристики).
    /// Вызывается фоновой службой и админкой после изменения каталога.
    /// </summary>
    public interface IVectorSearchIndexRebuildService
    {
        /// <summary>
        /// Пересоздаёт idx:products и все product:* hash-ключи.
        /// TODO: Оптимизировать до переиндексации одного товара (IndexProductAsync).
        /// </summary>
        Task RebuildFullIndexAsync(CancellationToken cancellationToken = default);
    }
}
