using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services.Background
{
    /// <summary>
    /// Background service that periodically cleans up stale processes and user activities.
    /// - Finds users with IsAvailable=false or non-empty CurrentActivities
    /// - If associated process is terminal → cleanup user immediately
    /// - If process is stale (older than timeout) and non-terminal → terminate as timeout
    /// </summary>
    public class StaleProcessCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StaleProcessCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _processTimeout = TimeSpan.FromMinutes(10);

        public StaleProcessCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<StaleProcessCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StaleProcessCleanupService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupStaleDataAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in StaleProcessCleanupService");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("StaleProcessCleanupService stopped");
        }

        private async Task CleanupStaleDataAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VoltyksDbContext>();
            var processesService = scope.ServiceProvider.GetRequiredService<IProcessesService>();

            // Find users with IsAvailable=false or non-empty CurrentActivities
            var usersToCheck = await ctx.Set<AppUser>()
                .Where(u => !u.IsAvailable || (u.CurrentActivitiesJson != null && u.CurrentActivitiesJson != "[]"))
                .ToListAsync(ct);

            if (usersToCheck.Count == 0)
                return;

            _logger.LogDebug("Checking {Count} users for stale activities", usersToCheck.Count);

            foreach (var user in usersToCheck)
            {
                try
                {
                    await ProcessUserCleanupAsync(ctx, processesService, user, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup user {UserId}", user.Id);
                }
            }

            await ctx.SaveChangesAsync(ct);
        }

        private async Task ProcessUserCleanupAsync(
            VoltyksDbContext ctx,
            IProcessesService processesService,
            AppUser user,
            CancellationToken ct)
        {
            var activityIds = user.CurrentActivities.ToList();

            // User has no activities but marked unavailable - fix it
            if (activityIds.Count == 0 && !user.IsAvailable)
            {
                user.IsAvailable = true;
                ctx.Update(user);
                _logger.LogInformation("Reset IsAvailable for user {UserId} with no activities", user.Id);
                return;
            }

            foreach (var processId in activityIds.ToList())
            {
                var process = await ctx.Set<Process>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == processId, ct);

                if (process == null)
                {
                    // Process doesn't exist - remove from activities
                    activityIds.Remove(processId);
                    user.CurrentActivities = activityIds;
                    ctx.Update(user);
                    _logger.LogInformation("Removed non-existent process {ProcessId} from user {UserId}", processId, user.Id);
                    continue;
                }

                var isTerminal = process.Status == ProcessStatus.Completed
                              || process.Status == ProcessStatus.Aborted
                              || process.Status == ProcessStatus.Disputed;

                if (isTerminal)
                {
                    // Process is terminal - cleanup immediately (idempotent call)
                    await processesService.TerminateProcessAsync(
                        processId,
                        process.Status, // Keep existing status
                        "cleanup",
                        null,
                        ct);
                    _logger.LogInformation("Cleaned up terminal process {ProcessId} for user {UserId}", processId, user.Id);
                }
                else
                {
                    // Check if process is stale (too old and still active)
                    var age = DateTime.UtcNow - process.DateCreated;
                    if (age > _processTimeout)
                    {
                        await processesService.TerminateProcessAsync(
                            processId,
                            ProcessStatus.Aborted,
                            "timeout",
                            null,
                            ct);
                        _logger.LogWarning("Terminated stale process {ProcessId} (age: {Age})", processId, age);
                    }
                }
            }

            // Final check: if all activities cleaned up, ensure user is available
            var updatedActivities = user.CurrentActivities.ToList();
            if (updatedActivities.Count == 0 && !user.IsAvailable)
            {
                user.IsAvailable = true;
                ctx.Update(user);
            }
        }
    }
}
