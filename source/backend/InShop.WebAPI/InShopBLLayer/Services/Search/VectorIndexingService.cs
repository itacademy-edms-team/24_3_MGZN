using InShopBLLayer.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InShopBLLayer.Services.Search
{
    /// <summary>
    /// Периодическая переиндексация каталога в Redis (каждый час).
    /// </summary>
    public class VectorIndexingService : BackgroundService
    {
        // BackgroundService — singleton; scoped-сервисы получаем через scope на каждый запуск.
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VectorIndexingService> _logger;
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

        public VectorIndexingService(
            IServiceScopeFactory scopeFactory,
            ILogger<VectorIndexingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Служба векторной индексации запущена");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var rebuildService = scope.ServiceProvider.GetRequiredService<IVectorSearchIndexRebuildService>();
                    await rebuildService.RebuildFullIndexAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка во время индексации векторов.");
                }

                _logger.LogInformation("Ожидание {Interval} до следующего запуска...", DefaultInterval);
                await Task.Delay(DefaultInterval, stoppingToken);
            }

            _logger.LogInformation("Служба векторной индексации остановлена.");
        }
    }
}
