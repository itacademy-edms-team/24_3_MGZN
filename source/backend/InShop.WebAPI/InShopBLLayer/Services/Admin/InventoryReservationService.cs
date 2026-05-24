using InShopBLLayer.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InShopBLLayer.Services.Admin
{
    /// <summary>
    /// Сервис резервирования остатков с оптимистичной блокировкой (RowVersion) и повторными попытками.
    /// </summary>
    public class InventoryReservationService : IInventoryReservationService
    {
        private const int MaxConcurrencyRetries = 3;

        private readonly AppDbContext _context;
        private readonly ILogger<InventoryReservationService> _logger;

        public InventoryReservationService(
            AppDbContext context,
            ILogger<InventoryReservationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task ReserveAsync(int productId, int quantity, CancellationToken ct = default)
        {
            ValidateQuantity(quantity);

            return ExecuteWithConcurrencyRetryAsync(
                productId,
                quantity,
                ct,
                operationName: "Reserve",
                applyChanges: (product, qty) =>
                {
                    // Доступно для резерва = только свободный пул (не зарезервированный остаток).
                    if (product.ProductStockQuantity < qty)
                    {
                        throw new InvalidOperationException(
                            $"Недостаточно свободного остатка для резервирования. " +
                            $"Товар {product.ProductId}: запрошено {qty}, доступно {product.ProductStockQuantity}.");
                    }

                    product.ProductStockQuantity -= qty;
                    product.ReservedQuantity += qty;
                });
        }

        public Task ReleaseAsync(int productId, int quantity, CancellationToken ct = default)
        {
            ValidateQuantity(quantity);

            return ExecuteWithConcurrencyRetryAsync(
                productId,
                quantity,
                ct,
                operationName: "Release",
                applyChanges: (product, qty) =>
                {
                    if (product.ReservedQuantity < qty)
                    {
                        throw new InvalidOperationException(
                            $"Нельзя освободить резерв: зарезервировано {product.ReservedQuantity}, запрошено {qty}. " +
                            $"Товар {product.ProductId}.");
                    }

                    product.ReservedQuantity -= qty;
                    product.ProductStockQuantity += qty;
                });
        }

        public Task FinalizeAsync(int productId, int quantity, CancellationToken ct = default)
        {
            ValidateQuantity(quantity);

            return ExecuteWithConcurrencyRetryAsync(
                productId,
                quantity,
                ct,
                operationName: "Finalize",
                applyChanges: (product, qty) =>
                {
                    if (product.ReservedQuantity < qty)
                    {
                        throw new InvalidOperationException(
                            $"Нельзя списать из резерва: зарезервировано {product.ReservedQuantity}, запрошено {qty}. " +
                            $"Товар {product.ProductId}. Сначала выполните ReserveAsync.");
                    }

                    // Товар покидает склад: резерв уменьшается, свободный пул не пополняется.
                    product.ReservedQuantity -= qty;
                });
        }

        private static void ValidateQuantity(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Количество должно быть больше нуля.");
            }
        }

        /// <summary>
        /// Выполняет изменение остатков внутри транзакции с повтором при DbUpdateConcurrencyException.
        ///
        /// Почему оптимистичная блокировка, а не pessimistic (UPDLOCK/HOLDLOCK):
        /// - не держим долгие блокировки строк в SQL Server — выше пропускная способность при чтении каталога;
        /// - конфликты при списании/резерве обычно редки; при гонке RowVersion несовпадёт и EF выбросит исключение;
        /// - цикл повторов перечитывает актуальную строку и пересчитывает дельту — типичный паттерн для Race Condition в e-commerce.
        ///
        /// RowVersion (SQL rowversion): при каждом UPDATE SQL Server меняет токен; WHERE включает старое значение,
        /// поэтому второй параллельный UPDATE с устаревшим токеном затрагивает 0 строк → DbUpdateConcurrencyException.
        /// </summary>
        private async Task ExecuteWithConcurrencyRetryAsync(
            int productId,
            int quantity,
            CancellationToken ct,
            string operationName,
            Action<Product, int> applyChanges)
        {
            for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

                    if (product is null)
                    {
                        throw new InvalidOperationException($"Товар с идентификатором {productId} не найден.");
                    }

                    applyChanges(product, quantity);

                    await _context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    if (attempt > 1)
                    {
                        _logger.LogInformation(
                            "Операция {Operation} для товара {ProductId} успешна с попытки {Attempt}.",
                            operationName,
                            productId,
                            attempt);
                    }

                    return;
                }
                catch (DbUpdateConcurrencyException ex) when (attempt < MaxConcurrencyRetries)
                {
                    await transaction.RollbackAsync(ct);

                    _logger.LogWarning(
                        ex,
                        "Конфликт оптимистичной блокировки ({Operation}, товар {ProductId}). " +
                        "Попытка {Attempt}/{Max}. Перезагрузка сущности и повтор.",
                        operationName,
                        productId,
                        attempt,
                        MaxConcurrencyRetries);

                    // Сбрасываем трекер: следующая итерация загрузит свежую строку с актуальным RowVersion.
                    _context.ChangeTracker.Clear();

                    // Небольшая пауза снижает «вечную гонку» при высокой конкуренции (опционально для production).
                    await Task.Delay(TimeSpan.FromMilliseconds(15 * attempt), ct);
                }
                catch (DbUpdateConcurrencyException ex) when (attempt == MaxConcurrencyRetries)
                {
                    await transaction.RollbackAsync(ct);
                    _context.ChangeTracker.Clear();

                    _logger.LogError(
                        ex,
                        "Не удалось выполнить {Operation} для товара {ProductId} после {Max} попыток.",
                        operationName,
                        productId,
                        MaxConcurrencyRetries);

                    throw new InvalidOperationException(
                        $"Не удалось обновить остатки товара {productId}: параллельное изменение данных. " +
                        "Повторите операцию позже.",
                        ex);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    _context.ChangeTracker.Clear();
                    throw;
                }
            }
        }
    }
}
