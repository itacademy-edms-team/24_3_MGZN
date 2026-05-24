using AutoMapper;
using Contracts.Admin.Dto;
using InShopBLLayer.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<AdminOrderService> _logger;

        public AdminOrderService(
            AppDbContext context,
            IMapper mapper,
            IInventoryReservationService inventoryReservationService,
            ILogger<AdminOrderService> logger)
        {
            _context = context;
            _mapper = mapper;
            _inventoryReservationService = inventoryReservationService;
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

                if (OrderStatusStateMachine.IsTerminalStatus(oldRaw))
                {
                    throw new InvalidOperationException(
                        "Статус нельзя изменить для завершённых или отменённых заказов (Delivered / Cancelled).");
                }

                OrderStatusStateMachine.ValidateTransition(oldRaw, canonicalNew);

                // TODO: Интегрировать InventoryReservationService (см. отдельную задачу).
                // Пример при подключении резерва в той же транзакции:
                // foreach (var item in order.OrderItems) {
                //   if (canonicalNew == Processing) await _inventoryReservationService.ReserveAsync(item.ProductId, item.QuantityItem, ct);
                //   if (canonicalNew == Cancelled) await _inventoryReservationService.ReleaseAsync(...);
                //   if (canonicalNew == Paid) await _inventoryReservationService.FinalizeAsync(...);
                // }

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

                return MapOrder(order);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
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
    }
}
