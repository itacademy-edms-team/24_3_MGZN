using AutoMapper;
using Contracts.Admin.Dto;
using InShopBLLayer.Abstractions;
using InShopDbModels.Data;
using InShopDbModels.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InShopBLLayer.Services.Admin
{
    public class AdminProductService : IAdminProductService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ProductImageStorage _imageStorage;
        private readonly IVectorSearchIndexRebuildService _searchIndexRebuild;
        private readonly ILogger<AdminProductService> _logger;

        public AdminProductService(
            AppDbContext context,
            IMapper mapper,
            ProductImageStorage imageStorage,
            IVectorSearchIndexRebuildService searchIndexRebuild,
            ILogger<AdminProductService> logger)
        {
            _context = context;
            _mapper = mapper;
            _imageStorage = imageStorage;
            _searchIndexRebuild = searchIndexRebuild;
            _logger = logger;
        }

        public async Task<PagedResultDto<AdminProductDto>> GetProductsAsync(int page, int pageSize, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Products
                .Include(p => p.ProductCategory)
                .AsNoTracking()
                .OrderBy(p => p.ProductId);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResultDto<AdminProductDto>
            {
                Items = _mapper.Map<List<AdminProductDto>>(items),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdminProductDto?> GetProductAsync(int id, CancellationToken ct = default)
        {
            var product = await _context.Products
                .Include(p => p.ProductCategory)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id, ct);

            return product is null ? null : _mapper.Map<AdminProductDto>(product);
        }

        public async Task<AdminProductDto> CreateProductAsync(AdminProductCreateDto dto, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                if (!await _context.Categories.AnyAsync(c => c.CategoryId == dto.ProductCategoryId, ct))
                {
                    throw new InvalidOperationException("Категория не найдена.");
                }

                var product = _mapper.Map<Product>(dto);
                product.ReservedQuantity = 0;

                if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                {
                    product.ImageUrl = await _imageStorage.SaveBase64ImageAsync(dto.ImageBase64, ct);
                }

                await _context.Products.AddAsync(product, ct);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                await _context.Entry(product).Reference(p => p.ProductCategory).LoadAsync(ct);
                var created = _mapper.Map<AdminProductDto>(product);
                await TriggerSearchIndexProductAsync(product.ProductId, ct);
                return created;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<AdminProductDto> UpdateProductAsync(int id, AdminProductUpdateDto dto, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductCategory)
                    .FirstOrDefaultAsync(p => p.ProductId == id, ct);

                if (product is null)
                {
                    throw new InvalidOperationException("Товар не найден.");
                }

                if (!await _context.Categories.AnyAsync(c => c.CategoryId == dto.ProductCategoryId, ct))
                {
                    throw new InvalidOperationException("Категория не найдена.");
                }

                _mapper.Map(dto, product);

                if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                {
                    var previousUrl = product.ImageUrl;
                    product.ImageUrl = await _imageStorage.SaveBase64ImageAsync(dto.ImageBase64, ct);
                    _imageStorage.TryDeleteProductImageFile(previousUrl);
                }
                else if (dto.RemoveImage)
                {
                    _imageStorage.TryDeleteProductImageFile(product.ImageUrl);
                    product.ImageUrl = null;
                }
                else if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    product.ImageUrl = dto.ImageUrl;
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                var updated = _mapper.Map<AdminProductDto>(product);
                await TriggerSearchIndexProductAsync(id, ct);
                return updated;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task DeleteProductAsync(int id, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id, ct);
                if (product is null)
                {
                    throw new InvalidOperationException("Товар не найден.");
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Товар {ProductId} удалён администратором", id);
                await TriggerSearchRemoveProductAsync(id, ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        /// <summary>
        /// Точечно обновляет документ товара в Redis Search после create/update.
        /// Ошибка индексации не откатывает сохранение в SQL — только логируется.
        /// </summary>
        private async Task TriggerSearchIndexProductAsync(int productId, CancellationToken ct)
        {
            _logger.LogInformation("Товар {ProductId} сохранён, запуск точечной индексации", productId);
            try
            {
                await _searchIndexRebuild.IndexProductAsync(productId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Точечная индексация Redis не удалась для товара {ProductId}", productId);
            }
        }

        /// <summary>
        /// Удаляет документ товара из Redis Search после delete в SQL.
        /// </summary>
        private async Task TriggerSearchRemoveProductAsync(int productId, CancellationToken ct)
        {
            _logger.LogInformation("Товар {ProductId} удалён, удаление из Redis-индекса", productId);
            try
            {
                await _searchIndexRebuild.RemoveProductAsync(productId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Удаление из Redis-индекса не удалось для товара {ProductId}", productId);
            }
        }
    }
}
