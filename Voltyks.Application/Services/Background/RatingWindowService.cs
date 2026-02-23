using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Application.Utilities;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Core.Enums;
using ChargingRequestEntity = Voltyks.Persistence.Entities.Main.ChargingRequest;
using ProcessEntity = Voltyks.Persistence.Entities.Main.Process;
using UserReportEntity = Voltyks.Persistence.Entities.Main.UserReport;

namespace Voltyks.Application.Services.Background
{
    /// <summary>
    /// Background service that polls for expired rating windows and applies
    /// default 3-star ratings for any party that did not submit a rating in time.
    /// </summary>
    public class RatingWindowService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RatingWindowService> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(60);
        private readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(5);
        private const double DefaultRating = 3.0;

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
                    await ProcessExpiredWindowsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RatingWindowService");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }

            _logger.LogInformation("RatingWindowService stopped");
        }

        private async Task ProcessExpiredWindowsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<VoltyksDbContext>();

            var cutoff = DateTime.UtcNow.Subtract(_windowDuration);

            // Find processes with expired rating windows that still need defaults
            var expiredProcessIds = await ctx.Set<ProcessEntity>()
                .AsNoTracking()
                .Where(p =>
                    p.RatingWindowOpenedAt != null &&
                    p.RatingWindowOpenedAt <= cutoff &&
                    !p.DefaultRatingApplied &&
                    (!p.VehicleOwnerRating.HasValue || !p.ChargerOwnerRating.HasValue))
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (expiredProcessIds.Count == 0)
                return;

            _logger.LogInformation("Found {Count} expired rating windows to process", expiredProcessIds.Count);

            foreach (var processId in expiredProcessIds)
            {
                try
                {
                    await ApplyDefaultRatingsAsync(ctx, processId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply default ratings for process {ProcessId}", processId);
                }
            }
        }

        private async Task ApplyDefaultRatingsAsync(VoltyksDbContext ctx, int processId, CancellationToken ct)
        {
            // Collect notification targets (populated inside transaction, sent after commit)
            var notificationTargets = new List<(string UserId, int ProcessId, int RequestId)>();

            using var tx = await ctx.Database.BeginTransactionAsync(ct);
            try
            {
                // Re-fetch inside transaction for fresh state
                var process = await ctx.Set<ProcessEntity>()
                    .FirstOrDefaultAsync(p => p.Id == processId, ct);

                if (process == null)
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                // Guard: already processed or both ratings exist
                if (process.DefaultRatingApplied ||
                    (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue))
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                // Race guard: skip if already finalized by SubmitRatingAsync
                if ((process.Status == ProcessStatus.Completed || process.Status == ProcessStatus.Disputed)
                    && process.SubStatus == null)
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                // Apply default for missing VehicleOwnerRating (rating given by CO to VO)
                if (!process.VehicleOwnerRating.HasValue)
                {
                    process.VehicleOwnerRating = DefaultRating;

                    await ctx.AddAsync(new RatingsHistory
                    {
                        ProcessId = process.Id,
                        RaterUserId = "system",
                        RateeUserId = process.VehicleOwnerId,
                        Stars = DefaultRating
                    }, ct);

                    // Update VehicleOwner's rating aggregate
                    var vo = await ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == process.VehicleOwnerId, ct);
                    if (vo != null)
                    {
                        vo.Rating = ((vo.Rating * vo.RatingCount) + DefaultRating) / (vo.RatingCount + 1);
                        vo.RatingCount += 1;
                    }

                    notificationTargets.Add((process.VehicleOwnerId, process.Id, process.ChargerRequestId));
                }

                // Apply default for missing ChargerOwnerRating (rating given by VO to CO)
                if (!process.ChargerOwnerRating.HasValue)
                {
                    process.ChargerOwnerRating = DefaultRating;

                    await ctx.AddAsync(new RatingsHistory
                    {
                        ProcessId = process.Id,
                        RaterUserId = "system",
                        RateeUserId = process.ChargerOwnerId,
                        Stars = DefaultRating
                    }, ct);

                    // Update ChargerOwner's rating aggregate
                    var co = await ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == process.ChargerOwnerId, ct);
                    if (co != null)
                    {
                        co.Rating = ((co.Rating * co.RatingCount) + DefaultRating) / (co.RatingCount + 1);
                        co.RatingCount += 1;
                    }

                    notificationTargets.Add((process.ChargerOwnerId, process.Id, process.ChargerRequestId));
                }

                // Mark defaults applied
                process.DefaultRatingApplied = true;
                process.SubStatus = null; // rating stage complete

                // Check if reported → final status is Disputed
                var hasReport = await ctx.Set<UserReportEntity>()
                    .AnyAsync(r => r.ProcessId == process.Id, ct);

                var finalStatus = hasReport ? ProcessStatus.Disputed : ProcessStatus.Completed;
                var finalReqStatus = hasReport ? "Disputed" : "Completed";

                process.Status = finalStatus;
                if (process.DateCompleted == null)
                    process.DateCompleted = DateTimeHelper.GetEgyptTime();

                // Update ChargingRequest status (atomic with process)
                var request = await ctx.Set<ChargingRequestEntity>()
                    .FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
                if (request != null)
                    request.Status = finalReqStatus;

                // Clean up CurrentActivities for both users
                foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                {
                    var user = await ctx.Set<AppUser>()
                        .FirstOrDefaultAsync(u => u.Id == uid, ct);

                    if (user != null)
                    {
                        var activities = user.CurrentActivities.ToList();
                        if (activities.Remove(processId))
                        {
                            user.CurrentActivities = activities;
                            ctx.Entry(user).Property(u => u.CurrentActivitiesJson).IsModified = true;
                        }

                        if (user.CurrentActivities.Count == 0 && !user.IsAvailable)
                        {
                            user.IsAvailable = true;
                            ctx.Entry(user).Property(u => u.IsAvailable).IsModified = true;
                        }
                    }
                }

                await ctx.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Applied default ratings for process {ProcessId} ({DefaultCount} defaults)",
                    processId, notificationTargets.Count);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            // Send notifications AFTER transaction commit
            await SendDefaultRatingNotificationsAsync(ctx, notificationTargets, ct);
        }

        private async Task SendDefaultRatingNotificationsAsync(
            VoltyksDbContext ctx,
            List<(string UserId, int ProcessId, int RequestId)> targets,
            CancellationToken ct)
        {
            if (targets.Count == 0)
                return;

            IFirebaseService? firebase = null;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                firebase = scope.ServiceProvider.GetRequiredService<IFirebaseService>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve IFirebaseService for default rating notifications");
                return;
            }

            foreach (var (userId, processId, requestId) in targets)
            {
                try
                {
                    var tokens = await ctx.Set<DeviceToken>()
                        .AsNoTracking()
                        .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Token))
                        .Select(t => t.Token)
                        .ToListAsync(ct);

                    if (tokens.Count == 0)
                        continue;

                    var extraData = new Dictionary<string, string>
                    {
                        ["processId"] = processId.ToString(),
                        ["requestId"] = requestId.ToString(),
                        ["NotificationType"] = "DefaultRating_Applied",
                        ["defaultRating"] = DefaultRating.ToString("0.#")
                    };

                    foreach (var token in tokens)
                    {
                        try
                        {
                            await firebase.SendNotificationAsync(
                                token,
                                "Default rating applied",
                                $"A default {DefaultRating:0.#}★ rating was applied for process #{processId}.",
                                requestId,
                                "DefaultRating_Applied",
                                extraData);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send default rating notification to token for user {UserId}", userId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send default rating notifications for user {UserId}", userId);
                }
            }
        }
    }
}
