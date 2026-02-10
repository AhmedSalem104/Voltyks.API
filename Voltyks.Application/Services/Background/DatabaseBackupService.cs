using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voltyks.Application.Services.Background.Backup;

namespace Voltyks.Application.Services.Background
{
    public class DatabaseBackupService : BackgroundService
    {
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly DatabaseBackupOptions _options;
        private readonly string _connectionString;

        public DatabaseBackupService(
            ILogger<DatabaseBackupService> logger,
            IOptions<DatabaseBackupOptions> options,
            IConfiguration configuration)
        {
            _logger = logger;
            _options = options.Value;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DatabaseBackupService started (scheduled at {Time} Egypt time)", _options.DailyBackupTime);

            if (!_options.Enabled)
            {
                _logger.LogInformation("DatabaseBackupService is disabled via configuration");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delay = CalculateDelayUntilNextRun();
                    _logger.LogInformation("Next backup scheduled in {Hours}h {Minutes}m",
                        (int)delay.TotalHours, delay.Minutes);

                    await Task.Delay(delay, stoppingToken);

                    await RunBackupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DatabaseBackupService");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("DatabaseBackupService stopped");
        }

        private TimeSpan CalculateDelayUntilNextRun()
        {
            var egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var nowEgypt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);

            if (!TimeSpan.TryParse(_options.DailyBackupTime, out var targetTime))
                targetTime = new TimeSpan(3, 0, 0);

            var todayTarget = nowEgypt.Date.Add(targetTime);
            var nextRun = nowEgypt < todayTarget ? todayTarget : todayTarget.AddDays(1);

            var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, egyptZone);
            var delay = nextRunUtc - DateTime.UtcNow;

            return delay < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delay;
        }

        private async Task RunBackupAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting daily database backup...");

            var executor = new BackupExecutor(_connectionString, _options, _logger);
            var result = await executor.ExecuteBackupAsync(ct);

            if (result.Success)
            {
                var sizeMb = result.FileSizeBytes / (1024.0 * 1024.0);
                _logger.LogInformation(
                    "Backup completed successfully: {File} ({Size:F2} MB, {Tables} tables, {Rows} rows, {Duration:F1}s)",
                    Path.GetFileName(result.FilePath), sizeMb,
                    result.TablesExported, result.TotalRowsExported,
                    result.Duration.TotalSeconds);
            }
            else
            {
                _logger.LogError(
                    "Backup completed with errors: {ErrorCount} errors. Errors: {Errors}",
                    result.Errors.Count, string.Join("; ", result.Errors));
            }
        }
    }
}
