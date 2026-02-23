namespace Voltyks.Application.Services.Background.Backup
{
    public class DatabaseBackupOptions
    {
        public const string SectionName = "DatabaseBackup";

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Time of day (Egypt Standard Time) to run backup. Format: "HH:mm".
        /// </summary>
        public string DailyBackupTime { get; set; } = "03:00";

        public int RetentionCount { get; set; } = 30;

        /// <summary>
        /// Root path for backup storage. Relative paths resolve from AppContext.BaseDirectory.
        /// </summary>
        public string BackupPath { get; set; } = "Backups";

        public int BatchSize { get; set; } = 5000;

        public int CommandTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// API key for external cron services (e.g. cron-job.org) to trigger backups.
        /// </summary>
        public string? CronApiKey { get; set; }
    }
}
