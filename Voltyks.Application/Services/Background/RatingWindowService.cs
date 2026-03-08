using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Processes;

namespace Voltyks.Application.Services.Background
{
    /// <summary>
    /// Background service that polls for expired rating windows and applies
    /// default 3-star ratings for any party that did not submit a rating in time.
    /// Delegates actual processing to IRatingWindowProcessor (also callable via cron endpoint).
    /// </summary>
    public class RatingWindowService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RatingWindowService> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(60);

        public RatingWindowService(
            IServiceScopeFactory scopeFactory,
            ILogger<RatingWindowService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RatingWindowService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IRatingWindowProcessor>();
                    _ = await processor.ProcessExpiredWindowsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RatingWindowService");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }

            _logger.LogInformation("RatingWindowService stopped");
        }
    }
}
