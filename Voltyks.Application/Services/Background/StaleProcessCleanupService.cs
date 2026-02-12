using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Application.Utilities;
using Voltyks.Core.Enums;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using ChargingRequestEntity = Voltyks.Persistence.Entities.Main.ChargingRequest;

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
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
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
            var firebaseService = scope.ServiceProvider.GetRequiredService<IFirebaseService>();

            // 1. Cleanup orphaned ChargingRequests (pending/accepted without Process)
            await CleanupOrphanedRequestsAsync(ctx, firebaseService, ct);

            // 2. Cleanup stale user activities
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
                    // Skip processes still in rating phase — let RatingWindowService handle them
                    if (process.SubStatus == "awaiting_rating")
                    {
                        _logger.LogDebug(
                            "Skipping process {ProcessId} for user {UserId}: awaiting_rating (handled by RatingWindowService)",
                            processId, user.Id);
                        continue;
                    }

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
                    // Skip if payment was made — timer no longer applies
                    if (process.AmountPaid != null && process.AmountPaid > 0)
                        continue;

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

        private async Task CleanupOrphanedRequestsAsync(
            VoltyksDbContext ctx,
            IFirebaseService firebaseService,
            CancellationToken ct)
        {
            var now = DateTimeHelper.GetEgyptTime();
            var pendingCutoff = now.AddMinutes(-5);     // 5 min for unanswered requests
            var acceptedCutoff = now.AddMinutes(-10);   // 10 min for unpaid accepted requests

            var orphanedRequests = await ctx.Set<ChargingRequestEntity>()
                .Where(r =>
                    !ctx.Set<Process>().Any(p => p.ChargerRequestId == r.Id) &&
                    (
                        (r.Status == "pending" && r.RequestedAt < pendingCutoff) ||
                        ((r.Status == "accepted" || r.Status == "confirmed") && (
                            (r.RespondedAt != null && r.RespondedAt < acceptedCutoff) ||
                            (r.RespondedAt == null && r.RequestedAt < acceptedCutoff)
                        ))
                    ))
                .ToListAsync(ct);

            if (orphanedRequests.Count == 0)
                return;

            _logger.LogInformation("Found {Count} orphaned requests to expire", orphanedRequests.Count);

            foreach (var req in orphanedRequests)
            {
                var previousStatus = req.Status;
                req.Status = "Expired";

                var extraData = new Dictionary<string, string>
                {
                    ["requestId"] = req.Id.ToString(),
                    ["NotificationType"] = NotificationTypes.Process_Terminated,
                    ["terminationReason"] = "expired",
                    ["terminatedAt"] = DateTime.UtcNow.ToString("o")
                };

                // Send notification to both users
                foreach (var userId in new[] { req.UserId, req.RecipientUserId })
                {
                    if (string.IsNullOrWhiteSpace(userId)) continue;

                    var user = await ctx.Set<AppUser>()
                        .Include(u => u.DeviceTokens)
                        .FirstOrDefaultAsync(u => u.Id == userId, ct);

                    if (user?.DeviceTokens?.Any() != true) continue;

                    foreach (var token in user.DeviceTokens)
                    {
                        try
                        {
                            await firebaseService.SendNotificationAsync(
                                token.Token,
                                "Process Terminated",
                                "The request has expired",
                                req.Id,
                                NotificationTypes.Process_Terminated,
                                extraData);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send expiry notification to token for request {RequestId}", req.Id);
                        }
                    }
                }

                _logger.LogWarning("Expired orphaned request {RequestId} (was: {Status})", req.Id, previousStatus);
            }

            await ctx.SaveChangesAsync(ct);
        }
    }
}
