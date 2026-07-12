using AutoMapper;
using Contracts.Admin.Dto;
using InShopBLLayer.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InShopBLLayer.Services.Admin
{
    /// <summary>
    /// Управление заказами в админ-панели: список, смена статуса по FSM, аудит.
    /// Покупательские OrderService / оплата не изменяются.
    /// </summary>
    public class AdminOrderService : IAdminOrderService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IInventoryReservationService _inventoryReservationService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AdminOrderService> _logger;

        public AdminOrderService(
            AppDbContext context,
            IMapper mapper,
            IInventoryReservationService inventoryReservationService,
            IServiceScopeFactory scopeFactory,
            ILogger<AdminOrderService> logger)
        {
            _context = context;
            _mapper = mapper;
            _inventoryReservationService = inventoryReservationService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<PagedResultDto<AdminOrderDto>> GetOrdersAsync(
            int page,
            int pageSize,
            string? statusFilter,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var normalized = OrderStatusStateMachine.Normalize(statusFilter);
                query = query.Where(o =>
                    o.OrderStatus == statusFilter
                    || o.OrderStatus == normalized
                    || (normalized == OrderStatusStateMachine.Unpaid && o.OrderStatus == "Unpayed")
                    || (normalized == OrderStatusStateMachine.Paid && o.OrderStatus == "Payed"));
            }
            else
            {
                // Список заказов без черновиков корзины
                query = query.Where(o => o.OrderStatus != OrderStatusStateMachine.Draft);
            }

            query = query.OrderByDescending(o => o.OrderId);

            var total = await query.CountAsync(ct);
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResultDto<AdminOrderDto>
            {
                Items = orders.Select(MapOrder).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResultDto<AdminOrderDto>> GetDraftOrdersAsync(int page, int pageSize, CancellationToken ct = default)
        {
            return await GetOrdersAsync(page, pageSize, OrderStatusStateMachine.Draft, ct);
        }

        public async Task<AdminOrderDto?> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            return order is null ? null : MapOrder(order);
        }

        public async Task<AdminOrderDetailDto?> GetOrderDetailsAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.ShipCompany)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            if (order is null)
            {
                return null;
            }

            var audit = await _context.OrderAuditLogs
                .AsNoTracking()
                .Where(a => a.OrderId == orderId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);

            return new AdminOrderDetailDto
            {
                OrderId = order.OrderId,
                OrderStatus = OrderStatusStateMachine.Normalize(order.OrderStatus),
                RawOrderStatus = order.OrderStatus,
                OrderDate = order.OrderDate,
                OrderTotalAmount = order.OrderTotalAmount,
                PayStatus = order.PayStatus,
                PayMethod = order.PayMethod,
                CustomerFullname = order.CustomerFullname,
                CustomerEmail = order.CustomerEmail,
                CustomerPhoneNumber = order.CustomerPhoneNumber,
                SessionId = order.SessionId,
                ShipAddress = order.ShipAddress,
                ShipDate = order.ShipDate,
                ShipMethod = order.ShipMethod,
                ShipCompanyName = order.ShipCompany?.ShipCompanyName,
                Items = order.OrderItems.Select(i => new AdminOrderItemDetailDto
                {
                    OrderItemId = i.OrderItemId,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.ProductName ?? $"#{i.ProductId}",
                    Quantity = i.QuantityItem,
                    UnitPrice = i.Price,
                    LineTotal = i.TotalPrice ?? i.Price * i.QuantityItem
                }).ToList(),
                StatusHistory = audit.Select(a => new AdminOrderAuditEntryDto
                {
                    CreatedAt = a.CreatedAt,
                    OldStatus = a.OldStatus,
                    NewStatus = a.NewStatus,
                    ChangedBy = a.ChangedBy
                }).ToList()
            };
        }

        public async Task<AdminOrderDto> ChangeOrderStatusAsync(
            int orderId,
            string newStatus,
            string adminEmail,
            CancellationToken ct = default)
        {
            var canonicalNew = OrderStatusStateMachine.Normalize(newStatus);

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

                if (order is null)
                {
                    throw new InvalidOperationException("Заказ не найден.");
                }

                var oldRaw = order.OrderStatus;
                var oldCanonical = OrderStatusStateMachine.Normalize(oldRaw);

                if (OrderStatusStateMachine.IsTerminalStatus(oldRaw))
                {
                    throw new InvalidOperationException(
                        "Статус нельзя изменить для завершённых или отменённых заказов (Delivered / Cancelled).");
                }

                OrderStatusStateMachine.ValidateTransition(oldRaw, canonicalNew);

                await ApplyInventoryTransitionAsync(order, oldCanonical, canonicalNew, ct);

                order.OrderStatus = canonicalNew;

                var audit = new OrderAuditLog
                {
                    OrderId = orderId,
                    OldStatus = oldRaw,
                    NewStatus = canonicalNew,
                    ChangedBy = adminEmail,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.OrderAuditLogs.AddAsync(audit, ct);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Заказ {OrderId}: {Old} → {New}, админ {Admin}",
                    orderId,
                    oldRaw,
                    canonicalNew,
                    adminEmail);

                // Письмо уходит в фоне — админка не ждёт SMTP.
                QueueStatusEmail(orderId, canonicalNew);

                return MapOrder(order);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        /// <summary>
        /// Ставит отправку письма в фон с отдельным DI-scope (scoped-сервисы не переживают HTTP-запрос).
        /// </summary>
        private void QueueStatusEmail(int orderId, string newStatus)
        {
            if (!OrderStatusLabels.ShouldSendStatusEmail(newStatus))
            {
                return;
            }

            var scopeFactory = _scopeFactory;
            var logger = _logger;

            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var notifier = scope.ServiceProvider.GetRequiredService<IOrderStatusEmailNotifier>();

                    var orderForEmail = await db.Orders
                        .AsNoTracking()
                        .Include(o => o.OrderItems)
                            .ThenInclude(i => i.Product)
                        .FirstOrDefaultAsync(o => o.OrderId == orderId);

                    if (orderForEmail is null)
                    {
                        logger.LogWarning(
                            "Фоновая отправка письма: заказ {OrderId} не найден",
                            orderId);
                        return;
                    }

                    await notifier.SendStatusUpdateAsync(orderForEmail);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Фоновая отправка письма для заказа {OrderId} не удалась",
                        orderId);
                }
            });
        }

        public IReadOnlyList<string> GetAllowedNextStatuses(string? currentStatus) =>
            OrderStatusStateMachine.GetAllowedNextStatuses(currentStatus);

        private AdminOrderDto MapOrder(Order order)
        {
            var dto = _mapper.Map<AdminOrderDto>(order);
            dto.RawOrderStatus = order.OrderStatus;
            dto.OrderStatus = OrderStatusStateMachine.Normalize(order.OrderStatus);
            dto.ItemsCount = order.OrderItems?.Count ?? 0;
            return dto;
        }

        private async Task ApplyInventoryTransitionAsync(
            Order order,
            string oldStatus,
            string newStatus,
            CancellationToken ct)
        {
            if (order.OrderItems.Count == 0)
            {
                return;
            }

            // Резерв ставится при входе в Processing или Paid (онлайн-оплата часто минует Processing).
            var shouldReserve =
                (string.Equals(newStatus, OrderStatusStateMachine.Processing, StringComparison.OrdinalIgnoreCase)
                 && !string.Equals(oldStatus, OrderStatusStateMachine.Processing, StringComparison.OrdinalIgnoreCase))
                || (string.Equals(newStatus, OrderStatusStateMachine.Paid, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(oldStatus, OrderStatusStateMachine.Unpaid, StringComparison.OrdinalIgnoreCase));

            if (shouldReserve)
            {
                await ReserveOrderItemsAsync(order, ct);
                return;
            }

            if (string.Equals(newStatus, OrderStatusStateMachine.Cancelled, StringComparison.OrdinalIgnoreCase)
                && IsReservedStatus(oldStatus))
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await LoadProductForInventoryAsync(item.ProductId, ct);
                    // Заказ мог стать Paid через оплату без резерва — освобождать нечего.
                    if (product.ReservedQuantity < item.QuantityItem)
                    {
                        continue;
                    }

                    product.ReservedQuantity -= item.QuantityItem;
                    product.ProductStockQuantity += item.QuantityItem;
                }

                return;
            }

            if (string.Equals(newStatus, OrderStatusStateMachine.Delivered, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await LoadProductForInventoryAsync(item.ProductId, ct);
                    // Если резерва нет (оплата минула Processing) — добираем из свободного остатка, затем списываем.
                    if (product.ReservedQuantity < item.QuantityItem)
                    {
                        var deficit = item.QuantityItem - product.ReservedQuantity;
                        if (product.ProductStockQuantity < deficit)
                        {
                            throw new InvalidOperationException(
                                $"Нельзя списать товар {product.ProductId}: " +
                                $"зарезервировано {product.ReservedQuantity}, свободно {product.ProductStockQuantity}, требуется {item.QuantityItem}.");
                        }

                        product.ProductStockQuantity -= deficit;
                        product.ReservedQuantity += deficit;
                    }

                    product.ReservedQuantity -= item.QuantityItem;
                }
            }
        }

        private async Task ReserveOrderItemsAsync(Order order, CancellationToken ct)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await LoadProductForInventoryAsync(item.ProductId, ct);
                if (product.ProductStockQuantity < item.QuantityItem)
                {
                    throw new InvalidOperationException(
                        $"Недостаточно свободного остатка для товара {product.ProductId}: " +
                        $"запрошено {item.QuantityItem}, доступно {product.ProductStockQuantity}.");
                }

                product.ProductStockQuantity -= item.QuantityItem;
                product.ReservedQuantity += item.QuantityItem;
            }
        }

        private async Task<Product> LoadProductForInventoryAsync(int productId, CancellationToken ct)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
            return product ?? throw new InvalidOperationException($"Товар с идентификатором {productId} не найден.");
        }

        private static bool IsReservedStatus(string status)
        {
            return string.Equals(status, OrderStatusStateMachine.Processing, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, OrderStatusStateMachine.Paid, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, OrderStatusStateMachine.Shipped, StringComparison.OrdinalIgnoreCase);
        }
    }
}
