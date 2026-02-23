using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Voltyks.Application.Services.Background.Backup;
using Voltyks.Core.DTOs;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/v1/admin/backup")]
    public class AdminBackupController : ControllerBase
    {
        private readonly DatabaseBackupOptions _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminBackupController> _logger;

        public AdminBackupController(
            IOptions<DatabaseBackupOptions> options,
            IConfiguration configuration,
            ILogger<AdminBackupController> logger)
        {
            _options = options.Value;
            _configuration = configuration;
            _logger = logger;
        }

        private string GetBackupDirectory()
        {
            return Path.IsPathRooted(_options.BackupPath)
                ? _options.BackupPath
                : Path.Combine(AppContext.BaseDirectory, _options.BackupPath);
        }

        /// <summary>
        /// POST /api/v1/admin/backup/trigger
        /// Trigger a manual database backup immediately
        /// </summary>
        [HttpPost("trigger")]
        public async Task<ActionResult<ApiResponse<object>>> TriggerBackup(CancellationToken ct)
        {
            _logger.LogInformation("Manual backup triggered by admin");

            var connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            var executor = new BackupExecutor(connectionString, _options, _logger);
            var result = await executor.ExecuteBackupAsync(ct);

            if (!result.Success)
            {
                return BadRequest(new ApiResponse<object>(
                    message: "Backup completed with errors",
                    status: false,
                    errors: result.Errors
                ));
            }

            var sizeMb = result.FileSizeBytes / (1024.0 * 1024.0);

            return Ok(new ApiResponse<object>(
                data: new
                {
                    fileName = Path.GetFileName(result.FilePath),
                    filePath = result.FilePath,
                    sizeMb = Math.Round(sizeMb, 2),
                    tablesExported = result.TablesExported,
                    totalRows = result.TotalRowsExported,
                    durationSeconds = Math.Round(result.Duration.TotalSeconds, 1),
                    tableRowCounts = result.TableRowCounts
                },
                message: $"Backup completed: {result.TablesExported} tables, {result.TotalRowsExported} rows, {sizeMb:F2} MB",
                status: true
            ));
        }

        /// <summary>
        /// POST /api/v1/admin/backup/cron?key={CronApiKey}
        /// External cron endpoint — validates API key instead of JWT.
        /// Give this URL to cron-job.org to trigger daily backups.
        /// </summary>
        [HttpPost("cron")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> CronBackup(
            [FromQuery] string key, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_options.CronApiKey) ||
                !string.Equals(key, _options.CronApiKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Cron backup attempt with invalid API key");
                return Unauthorized(new ApiResponse<object>(
                    message: "Invalid API key",
                    status: false
                ));
            }

            _logger.LogInformation("Cron backup triggered");

            var connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            var executor = new BackupExecutor(connectionString, _options, _logger);
            var result = await executor.ExecuteBackupAsync(ct);

            var sizeMb = result.FileSizeBytes / (1024.0 * 1024.0);

            return Ok(new ApiResponse<object>(
                data: new
                {
                    fileName = Path.GetFileName(result.FilePath),
                    sizeMb = Math.Round(sizeMb, 2),
                    tablesExported = result.TablesExported,
                    totalRows = result.TotalRowsExported,
                    durationSeconds = Math.Round(result.Duration.TotalSeconds, 1),
                    success = result.Success,
                    errors = result.Errors
                },
                message: result.Success
                    ? $"Backup OK: {result.TablesExported} tables, {result.TotalRowsExported} rows, {sizeMb:F2} MB"
                    : $"Backup completed with {result.Errors.Count} error(s)",
                status: result.Success
            ));
        }

        /// <summary>
        /// GET /api/v1/admin/backup/list
        /// List all existing backup files
        /// </summary>
        [HttpGet("list")]
        public ActionResult<ApiResponse<object>> ListBackups()
        {
            var backupDir = GetBackupDirectory();

            if (!Directory.Exists(backupDir))
            {
                return Ok(new ApiResponse<object>(
                    data: new { backupDirectory = backupDir, files = Array.Empty<object>() },
                    message: "No backups found (directory does not exist)",
                    status: true
                ));
            }

            var files = Directory.GetFiles(backupDir, "backup_*.zip")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Select(f => new
                {
                    fileName = f.Name,
                    sizeMb = Math.Round(f.Length / (1024.0 * 1024.0), 2),
                    createdAt = f.CreationTimeUtc
                })
                .ToList();

            return Ok(new ApiResponse<object>(
                data: new { backupDirectory = backupDir, count = files.Count, files },
                message: $"{files.Count} backup(s) found",
                status: true
            ));
        }

        /// <summary>
        /// GET /api/v1/admin/backup/download/{fileName}
        /// Download a specific backup file
        /// </summary>
        [HttpGet("download/{fileName}")]
        public IActionResult DownloadBackup(string fileName)
        {
            // Sanitize: only allow backup_*.zip pattern
            if (!fileName.StartsWith("backup_") || !fileName.EndsWith(".zip") || fileName.Contains(".."))
            {
                return BadRequest(new ApiResponse<object>(
                    message: "Invalid file name",
                    status: false
                ));
            }

            var backupDir = GetBackupDirectory();
            var filePath = Path.Combine(backupDir, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new ApiResponse<object>(
                    message: "Backup file not found",
                    status: false
                ));
            }

            return PhysicalFile(filePath, "application/zip", fileName);
        }
    }
}
