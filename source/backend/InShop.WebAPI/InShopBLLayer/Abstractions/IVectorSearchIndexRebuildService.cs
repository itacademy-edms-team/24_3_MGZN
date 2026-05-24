namespace InShopBLLayer.Abstractions
{
    /// <summary>
    /// Управление поисковым индексом Redis (вектор + характеристики).
    /// Точечные операции — для админки; полный rebuild — для фоновой службы и recovery.
    /// </summary>
    public interface IVectorSearchIndexRebuildService
    {
        /// <summary>
        /// Индексирует или обновляет один товар (HSET product:{id}) без пересоздания idx:products.
        /// </summary>
        Task IndexProductAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаляет документ товара из индекса (DEL product:{id}).
        /// </summary>
        Task RemoveProductAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Пересоздаёт idx:products и все product:* hash-ключи.
        /// </summary>
        Task RebuildFullIndexAsync(CancellationToken cancellationToken = default);
    }
}
