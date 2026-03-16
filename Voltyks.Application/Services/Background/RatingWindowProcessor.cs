using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public class RatingWindowProcessor : IRatingWindowProcessor
    {
        private readonly VoltyksDbContext _ctx;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RatingWindowProcessor> _logger;
        private readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(4);
        private const double DefaultRating = 3.0;

        public RatingWindowProcessor(
            VoltyksDbContext ctx,
            IServiceScopeFactory scopeFactory,
            ILogger<RatingWindowProcessor> logger)
        {
            _ctx = ctx;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<RatingProcessingResult> ProcessExpiredWindowsAsync(CancellationToken ct)
        {
            int expiredCount = 0;
            int stuckCount = 0;

            // ── Sweep 1: expired windows (at least one rating missing) ──
            var cutoff = DateTime.UtcNow.Subtract(_windowDuration);

            var expiredProcessIds = await _ctx.Set<ProcessEntity>()
                .AsNoTracking()
                .Where(p =>
                    p.RatingWindowOpenedAt != null &&
                    p.RatingWindowOpenedAt <= cutoff &&
                    !p.DefaultRatingApplied &&
                    (!p.VehicleOwnerRating.HasValue || !p.ChargerOwnerRating.HasValue))
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (expiredProcessIds.Count > 0)
            {
                _logger.LogInformation("Found {Count} expired rating windows to process", expiredProcessIds.Count);

                foreach (var processId in expiredProcessIds)
                {
                    try
                    {
                        await ApplyDefaultRatingsAsync(processId, ct);
                        expiredCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to apply default ratings for process {ProcessId}", processId);
                    }
                }
            }

            // ── Sweep 2: stuck processes (both rated but never finalized) ──
            // This happens when both users rate concurrently — each request only
            // sees its own rating at check time, so neither finalizes the process.
            var stuckProcessIds = await _ctx.Set<ProcessEntity>()
                .AsNoTracking()
                .Where(p =>
                    p.SubStatus == "awaiting_rating" &&
                    p.VehicleOwnerRating.HasValue &&
                    p.ChargerOwnerRating.HasValue &&
                    !p.DefaultRatingApplied)
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (stuckProcessIds.Count > 0)
            {
                _logger.LogInformation("Found {Count} stuck processes (both rated, not finalized)", stuckProcessIds.Count);

                foreach (var processId in stuckProcessIds)
                {
                    try
                    {
                        await FinalizeStuckProcessAsync(processId, ct);
                        stuckCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to finalize stuck process {ProcessId}", processId);
                    }
                }
            }

            return new RatingProcessingResult(expiredCount, stuckCount);
        }

        private async Task ApplyDefaultRatingsAsync(int processId, CancellationToken ct)
        {
            var notificationTargets = new List<(string UserId, int ProcessId, int RequestId)>();

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                // UPDLOCK prevents concurrent cron + BackgroundService from processing the same row
                var process = await _ctx.Set<ProcessEntity>()
                    .FromSqlRaw("SELECT * FROM [Process] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {0}", processId)
                    .FirstOrDefaultAsync(ct);

                if (process == null)
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                if (process.DefaultRatingApplied ||
                    (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue))
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                if ((process.Status == ProcessStatus.Completed || process.Status == ProcessStatus.Disputed)
                    && process.SubStatus == null)
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                if (process.Status == ProcessStatus.Aborted)
                {
                    _logger.LogInformation("Skipping aborted process {ProcessId}", processId);
                    await tx.RollbackAsync(ct);
                    return;
                }

                // VehicleOwnerRating = rating given by CO to VO.
                // If null → CO didn't rate VO → RaterUserId = CO (the missing rater)
                if (!process.VehicleOwnerRating.HasValue)
                {
                    process.VehicleOwnerRating = DefaultRating;

                    await _ctx.AddAsync(new RatingsHistory
                    {
                        ProcessId = process.Id,
                        RaterUserId = process.ChargerOwnerId,
                        RateeUserId = process.VehicleOwnerId,
                        Stars = DefaultRating
                    }, ct);

                    var vo = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == process.VehicleOwnerId, ct);
                    if (vo != null)
                    {
                        vo.Rating = ((vo.Rating * vo.RatingCount) + DefaultRating) / (vo.RatingCount + 1);
                        vo.RatingCount += 1;
                    }

                    notificationTargets.Add((process.VehicleOwnerId, process.Id, process.ChargerRequestId));
                }

                // ChargerOwnerRating = rating given by VO to CO.
                // If null → VO didn't rate CO → RaterUserId = VO (the missing rater)
                if (!process.ChargerOwnerRating.HasValue)
                {
                    process.ChargerOwnerRating = DefaultRating;

                    await _ctx.AddAsync(new RatingsHistory
                    {
                        ProcessId = process.Id,
                        RaterUserId = process.VehicleOwnerId,
                        RateeUserId = process.ChargerOwnerId,
                        Stars = DefaultRating
                    }, ct);

                    var co = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == process.ChargerOwnerId, ct);
                    if (co != null)
                    {
                        co.Rating = ((co.Rating * co.RatingCount) + DefaultRating) / (co.RatingCount + 1);
                        co.RatingCount += 1;
                    }

                    notificationTargets.Add((process.ChargerOwnerId, process.Id, process.ChargerRequestId));
                }

                process.DefaultRatingApplied = true;
                process.SubStatus = null;

                var hasReport = await _ctx.Set<UserReportEntity>()
                    .AnyAsync(r => r.ProcessId == process.Id, ct);

                var finalStatus = hasReport ? ProcessStatus.Disputed : ProcessStatus.Completed;
                var finalReqStatus = hasReport ? "Disputed" : "Completed";

                process.Status = finalStatus;
                if (process.DateCompleted == null)
                    process.DateCompleted = DateTimeHelper.GetEgyptTime();

                var request = await _ctx.Set<ChargingRequestEntity>()
                    .FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
                if (request != null)
                    request.Status = finalReqStatus;

                foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                {
                    var user = await _ctx.Set<AppUser>()
                        .FirstOrDefaultAsync(u => u.Id == uid, ct);

                    if (user != null)
                    {
                        var activities = user.CurrentActivities.ToList();
                        if (activities.Remove(processId))
                        {
                            user.CurrentActivities = activities;
                            _ctx.Entry(user).Property(u => u.CurrentActivitiesJson).IsModified = true;
                        }

                        if (user.CurrentActivities.Count == 0 && !user.IsAvailable)
                        {
                            user.IsAvailable = true;
                            _ctx.Entry(user).Property(u => u.IsAvailable).IsModified = true;
                        }
                    }
                }

                await _ctx.SaveChangesAsync(ct);
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

            await SendDefaultRatingNotificationsAsync(notificationTargets, ct);
        }

        /// <summary>
        /// Finalizes a process where both parties rated concurrently but neither
        /// request finalized (race condition). Ratings are real — only status is updated.
        /// </summary>
        private async Task FinalizeStuckProcessAsync(int processId, CancellationToken ct)
        {
            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                var process = await _ctx.Set<ProcessEntity>()
                    .FromSqlRaw("SELECT * FROM [Process] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {0}", processId)
                    .FirstOrDefaultAsync(ct);

                if (process == null)
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                // Re-verify stuck condition after acquiring lock
                if (process.SubStatus != "awaiting_rating" ||
                    !process.VehicleOwnerRating.HasValue ||
                    !process.ChargerOwnerRating.HasValue ||
                    process.DefaultRatingApplied)
                {
                    await tx.RollbackAsync(ct);
                    return;
                }

                // Finalize — ratings are real (not defaults), so don't touch DefaultRatingApplied
                var hasReport = await _ctx.Set<UserReportEntity>()
                    .AnyAsync(r => r.ProcessId == process.Id, ct);

                var finalStatus = hasReport ? ProcessStatus.Disputed : ProcessStatus.Completed;
                var finalReqStatus = hasReport ? "Disputed" : "Completed";

                process.Status = finalStatus;
                process.SubStatus = null; // rating stage complete

                if (process.DateCompleted == null)
                    process.DateCompleted = DateTimeHelper.GetEgyptTime();

                var request = await _ctx.Set<ChargingRequestEntity>()
                    .FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
                if (request != null)
                    request.Status = finalReqStatus;

                await _ctx.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Finalized stuck process {ProcessId} (both ratings existed, SubStatus was awaiting_rating)",
                    processId);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        private async Task SendDefaultRatingNotificationsAsync(
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
                    var tokens = await _ctx.Set<DeviceToken>()
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
