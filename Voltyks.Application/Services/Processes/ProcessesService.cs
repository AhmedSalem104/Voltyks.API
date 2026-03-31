using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.ChargingRequest;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Pagination;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Application.Interfaces.SignalR;
using Voltyks.Application.Utilities;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Process;
using Voltyks.Core.Enums;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using ChargingRequestEntity = Voltyks.Persistence.Entities.Main.ChargingRequest;
using ProcessEntity = Voltyks.Persistence.Entities.Main.Process;


namespace Voltyks.Core.DTOs.Processes
{
    public class ProcessesService : IProcessesService
    {
        private readonly VoltyksDbContext _ctx;
        private readonly IHttpContextAccessor _http;
        private readonly IFirebaseService _firebase;
        private readonly ILogger<ProcessesService> _logger;
        private readonly IRedisService _redisService;
        private readonly IPaginationService _paginationService;
        private readonly ISignalRService _signalRService;

        public ProcessesService(VoltyksDbContext ctx, IHttpContextAccessor http, IFirebaseService firebase, ILogger<ProcessesService> logger, IRedisService redisService, IPaginationService paginationService, ISignalRService signalRService)
        {
            _ctx = ctx; _http = http;
            _firebase = firebase;
            _logger = logger;
            _redisService = redisService;
            _paginationService = paginationService;
            _signalRService = signalRService;
        }

        //        await tx.CommitAsync(ct);

        //        // notification في الـ response — بدون extraData وبأرقام حقيقية
        //        var notification = new
        //        {
        //            notificationId = notifDto.NotificationId,
        //            requestId = notifDto.RequestId,
        //            recipientUserId = notifDto.RecipientUserId,
        //            title = notifDto.Title,
        //            body = notifDto.Body,
        //            notificationType = notifDto.NotificationType,
        //            sentAt = notifDto.SentAt,
        //            pushSentCount = notifDto.PushSentCount,
        //            processId = process.Id,
        //            estimatedPrice = process.EstimatedPrice,
        //            amountCharged = process.AmountCharged,
        //            amountPaid = process.AmountPaid
        //        };

        //        var full = await _ctx.Set<ProcessEntity>()
        //            .AsNoTracking()
        //            .Where(p => p.Id == process.Id)
        //            .Select(p => new
        //            {
        //                p.Id,
        //                p.ChargerRequestId,
        //                p.VehicleOwnerId,
        //                p.ChargerOwnerId,
        //                p.Status,
        //                p.EstimatedPrice,
        //                p.AmountCharged,
        //                p.AmountPaid,
        //                p.VehicleOwnerRating,
        //                p.ChargerOwnerRating,
        //                p.DateCreated,
        //                p.DateCompleted
        //            })
        //            .FirstOrDefaultAsync(ct);

        //        var payload = new
        //        {
        //            process = full,
        //            notification = notification
        //        };

        //        return new ApiResponse<object>(payload, "Process updated successfully", true);
        //    }
        //    catch (Exception ex)
        //    {
        //        await tx.RollbackAsync(ct);
        //        return new ApiResponse<object>("Failed to update process", false, new() { ex.Message });
        //    }
        //}
        public async Task<ApiResponse<object>> ConfirmByVehicleOwnerAsync(ConfirmByVehicleOwnerDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var req = await _ctx.Set<ChargingRequestEntity>()
                .Include(r => r.CarOwner)
                .Include(r => r.Charger).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(r => r.Id == dto.ChargerRequestId, ct);
            if (req is null) return new ApiResponse<object>("Charger request not found", false);

            if (req.UserId != me) return new ApiResponse<object>("Forbidden", false);

            // ✅ Prevent confirming aborted/rejected requests
            if (req.Status == "Aborted" || req.Status == "Rejected")
                return new ApiResponse<object>($"Cannot confirm: request was {req.Status}", false);

            // ✅ Only allow confirm when request is in valid state
            if (req.Status != "accepted" && req.Status != "Confirmed")
                return new ApiResponse<object>($"Cannot confirm: request status is {req.Status}", false);

            var exists = await _ctx.Set<ProcessEntity>()
                                   .AsNoTracking()
                                   .AnyAsync(p => p.ChargerRequestId == req.Id, ct);
            if (exists) return new ApiResponse<object>("Process already created for this request", false);

            var process = new ProcessEntity
            {
                ChargerRequestId = req.Id,
                VehicleOwnerId = req.UserId,
                ChargerOwnerId = req.RecipientUserId!,
                EstimatedPrice = dto.EstimatedPrice,
                AmountCharged = dto.AmountCharged,
                AmountPaid = dto.AmountPaid,
                Status = ProcessStatus.PendingCompleted,
                SubStatus = "awaiting_completion"
            };

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                await _ctx.AddAsync(process, ct);
                req.Status = "PendingCompleted";
                _ctx.Update(req);

                await _ctx.SaveChangesAsync(ct);

                var vo = await _ctx.Set<AppUser>().FindAsync(new object?[] { req.UserId }, ct);
                var co = await _ctx.Set<AppUser>().FindAsync(new object?[] { req.RecipientUserId }, ct);

                if (vo != null)
                {
                    var list = vo.CurrentActivities.ToList();
                    if (!list.Contains(process.Id))
                    {
                        list.Add(process.Id);
                        vo.CurrentActivities = list;
                    }
                    vo.IsAvailable = false; // Hide from search during active process
                    _ctx.Entry(vo).Property(u => u.CurrentActivitiesJson).IsModified = true;
                    _ctx.Entry(vo).Property(u => u.IsAvailable).IsModified = true;
                }

                if (co != null)
                {
                    var list = co.CurrentActivities.ToList();
                    if (!list.Contains(process.Id))
                    {
                        list.Add(process.Id);
                        co.CurrentActivities = list;
                    }
                    co.IsAvailable = false; // Hide from search during active process
                    _ctx.Entry(co).Property(u => u.CurrentActivitiesJson).IsModified = true;
                    _ctx.Entry(co).Property(u => u.IsAvailable).IsModified = true;
                }

                await _ctx.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // Reset complaint rate limit for both vehicle owner and charger owner
                await _redisService.RemoveAsync($"complaint_last:{process.VehicleOwnerId}");
                await _redisService.RemoveAsync($"complaint_last:{process.ChargerOwnerId}");

                var title = "Process confirmation pending";
                var body = $"Amount Charged: {process.AmountCharged:0.##} | Amount Paid: {process.AmountPaid:0.##}";

                // extraData للـ FCM فقط
                var extraData = new Dictionary<string, string>
                {
                    ["processId"] = process.Id.ToString(),
                    ["estimatedPrice"] = (process.EstimatedPrice ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["amountCharged"] = (process.AmountCharged ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["amountPaid"] = (process.AmountPaid ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["userRole"] = "charger_owner"
                };

                var notifDto = await SendAndPersistNotificationAsync(
                    receiverUserId: process.ChargerOwnerId,
                    requestId: req.Id,
                    title: title,
                    processId: process.Id,
                    body: body,
                    notificationType: NotificationTypes.VehicleOwner_CreateProcess,
                    userTypeId: 1,
                    ct: ct,
                    extraData: extraData
                );

                // SignalR Real-time notification
                await _signalRService.SendProcessCreatedAsync(process.Id, process.ChargerOwnerId, new
                {
                    processId = process.Id,
                    requestId = req.Id,
                    estimatedPrice = process.EstimatedPrice,
                    amountCharged = process.AmountCharged,
                    amountPaid = process.AmountPaid,
                    status = "PendingCompleted"
                }, ct);

                // 👇 ده اللي هيروح في data في الـ response
                var responseData = new
                {
                    notificationId = notifDto.NotificationId,
                    requestId = notifDto.RequestId,
                    recipientUserId = notifDto.RecipientUserId,
                    title = notifDto.Title,
                    body = notifDto.Body,
                    notificationType = notifDto.NotificationType,
                    sentAt = notifDto.SentAt,
                    pushSentCount = notifDto.PushSentCount,
                    processId = process.Id,
                    estimatedPrice = process.EstimatedPrice,
                    amountCharged = process.AmountCharged,
                    amountPaid = process.AmountPaid
                };

                return new ApiResponse<object>(
                    responseData,
                    "Process created & request moved to PendingCompleted",
                    true
                );
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>("Failed to start process", false, new() { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> UpdateProcessAsync(UpdateProcessDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var process = await _ctx.Set<ProcessEntity>()
                                    .FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
            if (process is null)
                return new ApiResponse<object>("Process not found", false);

            var isChargerOwner = process.ChargerOwnerId == me;
            var isVehicleOwner = process.VehicleOwnerId == me;
            if (!isChargerOwner && !isVehicleOwner)
                return new ApiResponse<object>("Forbidden", false);

            var request = await _ctx.Set<ChargingRequestEntity>()
                                    .FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
            if (request is null)
                return new ApiResponse<object>("Charger request not found", false);

            string? raw = dto.Status?.Trim();
            string? decision = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (raw.Equals("Process-Completed", StringComparison.OrdinalIgnoreCase)) decision = "completed";
                else if (raw.Equals("Process-Ended-By-Report", StringComparison.OrdinalIgnoreCase)) decision = "ended-by-report";
                else if (raw.Equals("Process-Started", StringComparison.OrdinalIgnoreCase)) decision = "started";
                else if (raw.Equals("Process-Aborted", StringComparison.OrdinalIgnoreCase)) decision = "aborted";
            }

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                // تحديث القيم (decimals)
                if (dto.EstimatedPrice.HasValue) process.EstimatedPrice = dto.EstimatedPrice;
                if (dto.AmountCharged.HasValue) process.AmountCharged = dto.AmountCharged;
                if (dto.AmountPaid.HasValue) process.AmountPaid = dto.AmountPaid;

                // Mark as updated so pending endpoint returns UpdateProcess
                if (dto.EstimatedPrice.HasValue || dto.AmountCharged.HasValue || dto.AmountPaid.HasValue)
                    process.SubStatus = "process_updated";

                // حالة العملية
                if (decision == "completed")
                {
                    process.Status = ProcessStatus.Completed;
                    process.SubStatus = "awaiting_rating";
                    process.RatingWindowOpenedAt = DateTime.UtcNow;
                    process.DateCompleted = DateTimeHelper.GetEgyptTime();
                    request.Status = "Completed";

                    // Free both users immediately — rating is post-session, shouldn't lock them
                    foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                    {
                        var user = await _ctx.Set<AppUser>().FindAsync(new object?[] { uid }, ct);
                        if (user != null)
                        {
                            var activities = user.CurrentActivities.ToList();
                            if (activities.Remove(process.Id))
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
                }
                else if (decision == "started")
                {
                    request.Status = "Started";
                    process.SubStatus = "charging_in_progress";
                }
                else if (decision == "aborted" || decision == "ended-by-report")
                {
                    // ✅ Use unified termination method
                    var reason = decision == "ended-by-report" ? "report" : "aborted";
                    var status = reason == "report" ? ProcessStatus.Disputed : ProcessStatus.Aborted;

                    await TerminateProcessAsync(process.Id, status, reason, me, ct);
                }

                _ctx.Update(process);
                _ctx.Update(request);
                await _ctx.SaveChangesAsync(ct);

                var title = "Process updated";
                var body = "The vehicle owner updated process details.";

                var changes = new List<string>();
                if (dto.Status != null) changes.Add($"status: {dto.Status}");
                if (dto.EstimatedPrice != null) changes.Add($"estimated: {dto.EstimatedPrice:0.##}");
                if (dto.AmountCharged != null) changes.Add($"charged: {dto.AmountCharged:0.##}");
                if (dto.AmountPaid != null) changes.Add($"paid: {dto.AmountPaid:0.##}");
                if (changes.Any())
                    body = "Updated fields → " + string.Join(", ", changes);

                // extraData للـ FCM فقط
                var extraData = new Dictionary<string, string>
                {
                    ["processId"] = process.Id.ToString(),
                    ["estimatedPrice"] = (process.EstimatedPrice ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["amountCharged"] = (process.AmountCharged ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["amountPaid"] = (process.AmountPaid ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ["userRole"] = "charger_owner"
                };

                var updateNotifType = decision switch
                {
                    "completed" => NotificationTypes.ChargerOwner_ConfirmedProcessSuccessfully,
                    "aborted" or "ended-by-report" => NotificationTypes.Process_Terminated,
                    _ => NotificationTypes.VehicleOwner_UpdateProcess
                };

                var notifDto = await SendAndPersistNotificationAsync(
                    receiverUserId: process.ChargerOwnerId,
                    requestId: process.ChargerRequestId,
                    processId: process.Id,
                    title: title,
                    body: body,
                    notificationType: updateNotifType,
                    userTypeId: 1,
                    ct: ct,
                    extraData: extraData
                );

                await tx.CommitAsync(ct);

                // SignalR Real-time notification based on decision
                if (decision == "completed")
                {
                    await _signalRService.SendPaymentCompletedAsync(process.Id, process.ChargerOwnerId, new
                    {
                        processId = process.Id,
                        status = "Completed"
                    }, ct);
                }
                else if (decision == "aborted" || decision == "ended-by-report")
                {
                    await _signalRService.SendPaymentAbortedAsync(process.Id, process.ChargerOwnerId, new
                    {
                        processId = process.Id,
                        status = "Aborted"
                    }, ct);
                }
                else if (decision == "started")
                {
                    await _signalRService.SendProcessStartedAsync(process.Id, process.ChargerOwnerId, new
                    {
                        processId = process.Id,
                        status = "Started"
                    }, ct);
                }
                else
                {
                    await _signalRService.SendPaymentStatusChangedAsync(process.Id, process.ChargerOwnerId, "updated", new
                    {
                        processId = process.Id,
                        estimatedPrice = process.EstimatedPrice,
                        amountCharged = process.AmountCharged,
                        amountPaid = process.AmountPaid
                    }, ct);
                }

                var responseData = new
                {
                    process = new
                    {
                        processId          = process.Id,
                        chargerRequestId   = process.ChargerRequestId,
                        vehicleOwnerId     = process.VehicleOwnerId,
                        chargerOwnerId     = process.ChargerOwnerId,
                        status             = process.Status.ToString(),
                        subStatus          = process.SubStatus,
                        estimatedPrice     = process.EstimatedPrice,
                        amountCharged      = process.AmountCharged,
                        amountPaid         = process.AmountPaid,
                        vehicleOwnerRating = process.VehicleOwnerRating,
                        chargerOwnerRating = process.ChargerOwnerRating,
                        dateCreated        = process.DateCreated,
                        dateCompleted      = process.DateCompleted
                    },
                    notification = new
                    {
                        notificationId   = notifDto.NotificationId,
                        requestId        = notifDto.RequestId,
                        recipientUserId  = notifDto.RecipientUserId,
                        title            = notifDto.Title,
                        body             = notifDto.Body,
                        notificationType = notifDto.NotificationType,
                        sentAt           = notifDto.SentAt,
                        pushSentCount    = notifDto.PushSentCount
                    }
                };

                return new ApiResponse<object>(
                    responseData,
                    "Process updated successfully",
                    true
                );
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>("Failed to update process", false, new() { ex.Message });
            }
        }


        public async Task<ApiResponse<object>> OwnerDecisionAsync(OwnerDecisionDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
            if (process is null) return new ApiResponse<object>("Process not found", false);

            var request = await _ctx.Set<ChargingRequestEntity>().FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
            if (request is null) return new ApiResponse<object>("Charger request not found", false);

            // مين اللي بياخد القرار؟
            var isChargerOwner = process.ChargerOwnerId == me;
            var isVehicleOwner = process.VehicleOwnerId == me;

            if (!isChargerOwner && !isVehicleOwner)
                return new ApiResponse<object>("Forbidden", false);

            // ⚙️ تطبيع القرار على القيم الجديدة - مرن لقبول أشكال متعددة
            var raw = (dto.Decision ?? "Process-Completed").Trim();
            // Flexible normalization - accepts multiple formats
            var decision = raw.Contains("abort", StringComparison.OrdinalIgnoreCase)
                             || raw.Contains("cancel", StringComparison.OrdinalIgnoreCase) ? "aborted"
                         : raw.Contains("report", StringComparison.OrdinalIgnoreCase) ? "ended-by-report"
                         : raw.Contains("start", StringComparison.OrdinalIgnoreCase) ? "started"
                         : "completed"; // Default for completed/confirm/etc.

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                if (decision == "completed")
                {
                    process.Status = ProcessStatus.Completed;
                    process.SubStatus = "awaiting_rating";
                    if (process.DateCompleted == null)
                        process.DateCompleted = DateTimeHelper.GetEgyptTime();
                    if (process.RatingWindowOpenedAt == null)
                        process.RatingWindowOpenedAt = DateTime.UtcNow;
                    request.Status = "Completed";

                    // Free both users immediately — rating is post-session, shouldn't lock them
                    foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                    {
                        var user = await _ctx.Set<AppUser>().FindAsync(new object?[] { uid }, ct);
                        if (user != null)
                        {
                            var activities = user.CurrentActivities.ToList();
                            if (activities.Remove(process.Id))
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

                    _ctx.Update(process);
                    _ctx.Update(request);
                    await _ctx.SaveChangesAsync(ct);

                    // إشعار للطرف التاني حسب مين اتخذ القرار
                    if (isChargerOwner)
                    {
                        await SendToUserAsync(
                            process.VehicleOwnerId,
                            "Process confirmed",
                            "Charger owner confirmed your session. Please submit your rating.",
                            request.Id,
                            "ChargerOwner_ConfirmProcess",
                            ct,
                            new Dictionary<string, string> { ["userRole"] = "vehicle_owner" }
                        );
                        // SignalR Real-time
                        await _signalRService.SendPaymentCompletedAsync(process.Id, process.VehicleOwnerId, new
                        {
                            processId = process.Id,
                            status = "Completed",
                            confirmedBy = "charger_owner"
                        }, ct);
                    }
                    else // Vehicle Owner
                    {
                        await SendToUserAsync(
                            process.ChargerOwnerId,
                            "Process confirmed",
                            "Vehicle owner confirmed the session completion.",
                            request.Id,
                            "VehicleOwner_ConfirmProcess",
                            ct,
                            new Dictionary<string, string> { ["userRole"] = "charger_owner" }
                        );
                        // SignalR Real-time
                        await _signalRService.SendPaymentCompletedAsync(process.Id, process.ChargerOwnerId, new
                        {
                            processId = process.Id,
                            status = "Completed",
                            confirmedBy = "vehicle_owner"
                        }, ct);
                    }
                }
                else if (decision == "started")
                {
                    // بدء العملية: بنعلّم الطلب إنها بدأت
                    // لو عندك ProcessStatus.Started استخدمه؛ غير كده هنسيب Status زي ما هو ونعلم الطلب
                    request.Status = "Started";
                    process.SubStatus = "charging_in_progress";
                    _ctx.Update(process);
                    _ctx.Update(request);
                    await _ctx.SaveChangesAsync(ct);

                    // إشعار للطرف الآخر ببدء العملية
                    var receiverId = isChargerOwner ? process.VehicleOwnerId : process.ChargerOwnerId;
                    var whoStarted = isChargerOwner ? "Charger owner" : "Vehicle owner";
                    await SendToUserAsync(
                        receiverId,
                        "Process started",
                        $"{whoStarted} started the process.",
                        request.Id,
                        "Process_Started",
                        ct,
                        new Dictionary<string, string> { ["userRole"] = isChargerOwner ? "vehicle_owner" : "charger_owner" }
                    );
                    // SignalR Real-time
                    await _signalRService.SendProcessStartedAsync(process.Id, receiverId, new
                    {
                        processId = process.Id,
                        status = "Started",
                        startedBy = isChargerOwner ? "charger_owner" : "vehicle_owner"
                    }, ct);
                }
                else // ended-by-report | aborted  -> نفس مسار الإنهاء/التبليغ
                {
                    // ✅ Use unified termination method
                    var reason = decision.ToLower().Contains("report") ? "report" : "aborted";
                    var status = reason == "report" ? ProcessStatus.Disputed : ProcessStatus.Aborted;

                    await TerminateProcessAsync(process.Id, status, reason, me, ct);
                    await _ctx.SaveChangesAsync(ct);

                    // SignalR Real-time to counterparty
                    var receiverId = isChargerOwner ? process.VehicleOwnerId : process.ChargerOwnerId;
                    await _signalRService.SendPaymentAbortedAsync(process.Id, receiverId, new
                    {
                        processId = process.Id,
                        status = status.ToString(),
                        abortedBy = isChargerOwner ? "charger_owner" : "vehicle_owner"
                    }, ct);
                }

                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>("Failed to apply decision", false, new() { ex.Message });
            }

            var who = isChargerOwner ? "ChargerOwner" : "VehicleOwner";
            string msg, statusText;

            if (decision == "completed")
            {
                msg = "Process confirmed";
                statusText = process.Status.ToString();
            }
            else if (decision == "started")
            {
                msg = "Process started";
                statusText = request.Status; // "Started"
            }
            else
            {
                msg = "Process reported (aborted)";
                statusText = process.Status.ToString();
            }

            return new ApiResponse<object>(
                new { processId = process.Id, status = statusText, decidedBy = who, decision = raw },
                msg,
                true
            );
        }

        //public async Task<ApiResponse<object>> SubmitRatingAsync(SubmitRatingDto dto, CancellationToken ct = default)
        //{


        //    var me = CurrentUserId();
        //    if (string.IsNullOrEmpty(me))
        //        return new ApiResponse<object>("Unauthorized", false);

        //    var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
        //    if (process is null) return new ApiResponse<object>("Process not found", false);

        //    if (dto.RatingForOther < 1 || dto.RatingForOther > 5)
        //        return new ApiResponse<object>("Invalid rating value (1..5)", false);

        //    // مين بيقيّم مين؟
        //    var raterId = me;
        //    var rateeId = (process.VehicleOwnerId == me) ? process.ChargerOwnerId
        //               : (process.ChargerOwnerId == me) ? process.VehicleOwnerId
        //               : null;
        //    if (rateeId is null) return new ApiResponse<object>("Forbidden", false);

        //    // منع التقييم المكرر لنفس الشخص على نفس الـ Process
        //    var already = await _ctx.Set<RatingsHistory>()
        //        .AnyAsync(x => x.ProcessId == process.Id && x.RaterUserId == raterId, ct);
        //    if (already) return new ApiResponse<object>("You already rated this process", false);

        //    // خزّن التقييم داخل الـ Process (المصدر المعتمد للعرض)
        //    if (me == process.VehicleOwnerId)
        //        process.ChargerOwnerRating = dto.RatingForOther;   // VO يقيّم CO
        //    else
        //        process.VehicleOwnerRating = dto.RatingForOther;   // CO يقيّم VO

        //    // توثيق في الـ History (اختياري لكن مفيد للأرشفة)
        //    await _ctx.AddAsync(new RatingsHistory
        //    {
        //        ProcessId = process.Id,
        //        RaterUserId = raterId,
        //        RateeUserId = rateeId!,
        //        Stars = dto.RatingForOther
        //    }, ct);

        //    // تحديث المتوسط العام للمستخدم المُقَيَّم
        //    var ratee = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == rateeId, ct);
        //    ratee!.Rating = ((ratee.Rating * ratee.RatingCount) + dto.RatingForOther) / (ratee.RatingCount + 1);
        //    ratee.RatingCount += 1;

        //    // لو الاتنين قيّموا، أنهِ العملية
        //    if (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue)
        //    {
        //        process.Status = ProcessStatus.Completed;
        //        process.DateCompleted = GetEgyptTime();

        //        // (اختياري) شيلها من CurrentActivities
        //        foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
        //        {
        //            var u = await _ctx.Set<AppUser>().FindAsync(new object?[] { uid }, ct);
        //            if (u != null)
        //            {
        //                var list = u.CurrentActivities.ToList();
        //                if (list.Contains(process.Id)) { list.Remove(process.Id); u.CurrentActivities = list; _ctx.Update(u); }
        //            }
        //        }
        //    }

        //    await _ctx.SaveChangesAsync(ct);
        //    // 📣 إرسال إشعار للطرف الآخر بالـ Rating الجديد (بدون تغيير أي لوجك سابق)
        //    string receiverUserId = rateeId!;
        //    bool receiverIsChargerOwner = receiverUserId == process.ChargerOwnerId;
        //    int userTypeId = receiverIsChargerOwner ? 1 : 2; // 1 = ChargerOwner, 2 = VehicleOwner

        //    var title = "New rating received ⭐";
        //    var body = $"You received a {dto.RatingForOther:0.#}★ rating for process #{process.Id}.";
        //    var notificationType = receiverIsChargerOwner
        //        ? "VehicleOwner_SubmitRating"   // VO قيّم CO
        //        : "ChargerOwner_SubmitRating";  // CO قيّم VO

        //    // لو عندك ChargerRequestId جوه الـ process (منشأ من ConfirmByVehicleOwnerAsync)
        //    var relatedRequestId = process.ChargerRequestId;

        //    // نفس شكل الـ payload الراجعة من ChargingRequestService.SendAndPersistNotificationAsync
        //    var ratingNotifDto = await SendAndPersistNotificationAsync(
        //        receiverUserId: receiverUserId,
        //        requestId: relatedRequestId,
        //        processId:process.Id,
        //        title: title,
        //        body: body,
        //        notificationType: notificationType,
        //        userTypeId: userTypeId,
        //        ct: ct
        //    );



        //    // ⬇️ ارجع التقييمين من جدول Process نفسه
        //    double? yourRatingForOther;
        //    double? otherRatingForYou;

        //    if (me == process.VehicleOwnerId)
        //    {
        //        yourRatingForOther = process.ChargerOwnerRating;   // انت VO → قيّمْت CO
        //        otherRatingForYou = process.VehicleOwnerRating;   // تقييم CO ليك (VO)
        //    }
        //    else
        //    {
        //        yourRatingForOther = process.VehicleOwnerRating;   // انت CO → قيّمْت VO
        //        otherRatingForYou = process.ChargerOwnerRating;   // تقييم VO ليك (CO)
        //    }

        //    return new ApiResponse<object>(new
        //    {
        //        processId = process.Id,
        //        processStatus = process.Status.ToString(),
        //        yourRatingForOther,
        //        otherRatingForYou // ممكن تكون null لو الطرف الآخر لسه ما قيّمش
        //    }, "Rating submitted", true);
        //}
        public async Task<ApiResponse<object>> SubmitRatingAsync(SubmitRatingDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
            if (process is null) return new ApiResponse<object>("Process not found", false);

            if (dto.RatingForOther < 1 || dto.RatingForOther > 5)
                return new ApiResponse<object>("Invalid rating value (1..5)", false);

            // مين بيقيّم مين؟
            var raterId = me;
            var rateeId = (process.VehicleOwnerId == me) ? process.ChargerOwnerId
                       : (process.ChargerOwnerId == me) ? process.VehicleOwnerId
                       : null;
            if (rateeId is null) return new ApiResponse<object>("Forbidden", false);

            // منع التقييم المكرر لنفس الشخص على نفس الـ Process
            var already = await _ctx.Set<RatingsHistory>()
                .AsNoTracking()
                .AnyAsync(x => x.ProcessId == process.Id && x.RaterUserId == raterId, ct);
            if (already) return new ApiResponse<object>("You already rated this process", false);

            // Reject rating if defaults already applied (window expired)
            if (process.DefaultRatingApplied)
                return new ApiResponse<object>("Rating window has expired — default ratings applied", false);

            // خزّن التقييم داخل الـ Process (المصدر المعتمد للعرض)
            if (me == process.VehicleOwnerId)
                process.ChargerOwnerRating = dto.RatingForOther;   // VO يقيّم CO
            else
                process.VehicleOwnerRating = dto.RatingForOther;   // CO يقيّم VO

            // توثيق في الـ History (اختياري لكن مفيد للأرشفة)
            await _ctx.AddAsync(new RatingsHistory
            {
                ProcessId = process.Id,
                RaterUserId = raterId,
                RateeUserId = rateeId!,
                Stars = dto.RatingForOther
            }, ct);

            // تحديث المتوسط العام للمستخدم المُقَيَّم
            var ratee = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == rateeId, ct);
            ratee!.Rating = ((ratee.Rating * ratee.RatingCount) + dto.RatingForOther) / (ratee.RatingCount + 1);
            ratee.RatingCount += 1;

            // لو الاتنين قيّموا، أنهِ العملية
            if (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue)
            {
                // Race guard: skip only if RatingWindowService already finalized (applied defaults)
                if (!process.DefaultRatingApplied)
                {
                    // Check if this process has a pending report → Disputed instead of Completed
                    var hasReport = await _ctx.Set<UserReport>()
                        .AnyAsync(r => r.ProcessId == process.Id, ct);

                    var finalStatus = hasReport ? ProcessStatus.Disputed : ProcessStatus.Completed;
                    var finalReqStatus = hasReport ? "Disputed" : "Completed";

                    process.Status = finalStatus;
                    process.SubStatus = null; // rating stage complete
                    if (process.DateCompleted == null)
                        process.DateCompleted = DateTimeHelper.GetEgyptTime();

                    // Update ChargingRequest status (atomic with process)
                    var request = await _ctx.Set<ChargingRequestEntity>()
                        .FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
                    if (request != null)
                        request.Status = finalReqStatus;

                    // شيلها من CurrentActivities
                    foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                    {
                        var u = await _ctx.Set<AppUser>().FindAsync(new object?[] { uid }, ct);
                        if (u != null)
                        {
                            var list = u.CurrentActivities.ToList();
                            if (list.Remove(process.Id))
                            {
                                u.CurrentActivities = list;
                                _ctx.Entry(u).Property(x => x.CurrentActivitiesJson).IsModified = true;
                            }
                            if (u.CurrentActivities.Count == 0 && !u.IsAvailable)
                            {
                                u.IsAvailable = true;
                                _ctx.Entry(u).Property(x => x.IsAvailable).IsModified = true;
                            }
                        }
                    }
                }
            }

            // Safety net: free the rater from this process
            // (normally freed at completion, but guards against transaction rollback)
            var rater = await _ctx.Set<AppUser>().FindAsync(new object?[] { me }, ct);
            if (rater != null)
            {
                var raterList = rater.CurrentActivities.ToList();
                if (raterList.Remove(process.Id))
                {
                    rater.CurrentActivities = raterList;
                    _ctx.Entry(rater).Property(x => x.CurrentActivitiesJson).IsModified = true;
                }
                if (rater.CurrentActivities.Count == 0 && !rater.IsAvailable)
                {
                    rater.IsAvailable = true;
                    _ctx.Entry(rater).Property(x => x.IsAvailable).IsModified = true;
                }
            }

            await _ctx.SaveChangesAsync(ct);

            // 📣 إرسال إشعار للطرف الآخر بالـ Rating الجديد
            string receiverUserId = rateeId!;
            bool receiverIsChargerOwner = receiverUserId == process.ChargerOwnerId;
            int userTypeId = receiverIsChargerOwner ? 1 : 2; // 1 = ChargerOwner, 2 = VehicleOwner

            var title = "New rating received ⭐";
            var body = $"You received a {dto.RatingForOther:0.#}★ rating for process #{process.Id}.";
            var notificationType = receiverIsChargerOwner
                ? "VehicleOwner_SubmitRating"   // VO قيّم CO
                : "ChargerOwner_SubmitRating";  // CO قيّم VO

            var relatedRequestId = process.ChargerRequestId;

            var ratingExtraData = new Dictionary<string, string>
            {
                ["userRole"] = receiverIsChargerOwner ? "charger_owner" : "vehicle_owner"
            };

            var ratingNotifDto = await SendAndPersistNotificationAsync(
                receiverUserId: receiverUserId,
                requestId: relatedRequestId,
                processId: process.Id,
                title: title,
                body: body,
                notificationType: notificationType,
                userTypeId: userTypeId,
                ct: ct,
                extraData: ratingExtraData
            );

            // ⬇ ارجع التقييمين من جدول Process نفسه
            double? yourRatingForOther;
            double? otherRatingForYou;

            if (me == process.VehicleOwnerId)
            {
                yourRatingForOther = process.ChargerOwnerRating;   // انت VO → قيّمْت CO
                otherRatingForYou = process.VehicleOwnerRating;   // تقييم CO ليك (VO)
            }
            else
            {
                yourRatingForOther = process.VehicleOwnerRating;   // انت CO → قيّمْت VO
                otherRatingForYou = process.ChargerOwnerRating;  // تقييم VO ليك (CO)
            }

            // ✅ نفس شكل create/update/report: data = notification + extra fields
            var responseData = new
            {
                notificationId = ratingNotifDto.NotificationId,
                requestId = ratingNotifDto.RequestId,
                recipientUserId = ratingNotifDto.RecipientUserId,
                title = ratingNotifDto.Title,
                body = ratingNotifDto.Body,
                notificationType = ratingNotifDto.NotificationType,
                sentAt = ratingNotifDto.SentAt,
                pushSentCount = ratingNotifDto.PushSentCount,

                processId = process.Id,
                processStatus = process.Status.ToString(),
                yourRatingForOther,
                otherRatingForYou
            };

            return new ApiResponse<object>(responseData, "Rating submitted", true);
        }

        public async Task<ApiResponse<object>> GetRatingsSummaryAsync(int Id, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var p = await _ctx.Set<ProcessEntity>()
                              .AsNoTracking()
                              .FirstOrDefaultAsync(x => x.Id == Id, ct);

            if (p is null)
                return new ApiResponse<object>("Process not found", false);

            // Privacy
            if (p.VehicleOwnerId != me && p.ChargerOwnerId != me)
                return new ApiResponse<object>("Forbidden", false);

            double? yourRatingForOther;
            double? otherRatingForYou;

            if (p.VehicleOwnerId == me)
            {
                // أنت صاحب المركبة → تقييمك للـ ChargerOwner محفوظ في ChargerOwnerRating
                yourRatingForOther = p.ChargerOwnerRating;
                // تقييم الآخر لك محفوظ في VehicleOwnerRating
                otherRatingForYou = p.VehicleOwnerRating;
            }
            else
            {
                // أنت صاحب المحطة → تقييمك للـ VehicleOwner محفوظ في VehicleOwnerRating
                yourRatingForOther = p.VehicleOwnerRating;
                // تقييم الآخر لك محفوظ في ChargerOwnerRating
                otherRatingForYou = p.ChargerOwnerRating;
            }

            return new ApiResponse<object>(new
            {
                Id,
                yourRatingForOther,
                otherRatingForYou,
                hasBoth = yourRatingForOther.HasValue && otherRatingForYou.HasValue
            }, "Ratings summary", true);
        }

        private static readonly TimeSpan RatingWindowDuration = TimeSpan.FromMinutes(4);

        public async Task<ApiResponse<object>> OpenRatingWindowAsync(OpenRatingWindowDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var process = await _ctx.Set<ProcessEntity>()
                .FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);

            if (process is null)
                return new ApiResponse<object>("Process not found", false);

            // Auth: user must be VO or CO
            if (process.VehicleOwnerId != me && process.ChargerOwnerId != me)
                return new ApiResponse<object>("Forbidden", false);

            // Status gating - Aborted is terminal, Disputed allows rating (after report)
            if (process.Status == ProcessStatus.Aborted)
                return new ApiResponse<object>("Process is terminated", false);

            if (process.DefaultRatingApplied)
                return new ApiResponse<object>("Rating window has expired", false);

            if (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue)
                return new ApiResponse<object>("Both ratings already submitted", false);

            // Strict SubStatus check:
            // ALLOW: Completed && SubStatus == "awaiting_rating" (normal flow)
            // ALLOW: PendingCompleted (lenient — frontend opened rating screen early)
            // ALLOW: Disputed (after report — allow rating)
            // REJECT: Completed && SubStatus == null (already finalized)
            var isCompleted = process.Status == ProcessStatus.Completed;
            var isPendingCompleted = process.Status == ProcessStatus.PendingCompleted;
            var isDisputed = process.Status == ProcessStatus.Disputed;

            if (isCompleted && process.SubStatus != "awaiting_rating")
                return new ApiResponse<object>("Process is already finalized", false);

            if (!isCompleted && !isPendingCompleted && !isDisputed)
                return new ApiResponse<object>("Process is not in a ratable state", false);

            // Idempotency: if window already opened, return existing info without DB write
            if (process.RatingWindowOpenedAt.HasValue)
            {
                return new ApiResponse<object>(new
                {
                    processId = process.Id,
                    ratingWindowOpenedAt = process.RatingWindowOpenedAt.Value,
                    ratingWindowExpiresAt = process.RatingWindowOpenedAt.Value.Add(RatingWindowDuration),
                    alreadyOpened = true
                }, "Rating window already open", true);
            }

            // Open the rating window
            process.RatingWindowOpenedAt = DateTime.UtcNow;

            // Self-healing fallback: if process is still PendingCompleted,
            // complete it properly so the rating window and sweeps work correctly.
            if (process.Status == ProcessStatus.PendingCompleted)
            {
                process.Status = ProcessStatus.Completed;
                process.SubStatus = "awaiting_rating";
                process.DateCompleted ??= DateTimeHelper.GetEgyptTime();

                var request = await _ctx.Set<ChargingRequestEntity>()
                    .FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
                if (request != null)
                    request.Status = "Completed";

                foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                {
                    var user = await _ctx.Set<AppUser>().FindAsync(new object?[] { uid }, ct);
                    if (user != null)
                    {
                        var activities = user.CurrentActivities.ToList();
                        if (activities.Remove(process.Id))
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
            }

            _ctx.Update(process);
            await _ctx.SaveChangesAsync(ct);

            return new ApiResponse<object>(new
            {
                processId = process.Id,
                ratingWindowOpenedAt = process.RatingWindowOpenedAt.Value,
                ratingWindowExpiresAt = process.RatingWindowOpenedAt.Value.Add(RatingWindowDuration),
                alreadyOpened = false
            }, "Rating window opened", true);
        }

        public async Task<ApiResponse<object>> GetMyActivitiesAsync(PaginationParams? paginationParams, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var query = _ctx.Set<ProcessEntity>()
                .AsNoTracking()
                .Where(p => p.VehicleOwnerId == me || p.ChargerOwnerId == me)
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new MyActivityDto
                {
                    Id = p.Id,
                    ChargerRequestId = p.ChargerRequestId,
                    Status = p.Status.ToString(),
                    AmountCharged = p.AmountCharged,
                    AmountPaid = p.AmountPaid,
                    DateCreated = p.DateCreated,
                    DateCompleted = p.DateCompleted,
                    IsAsChargerOwner = (p.ChargerOwnerId == me),
                    IsAsVehicleOwner = (p.VehicleOwnerId == me),
                    Direction = (p.ChargerOwnerId == me) ? "Incoming" : "Outgoing",
                    CounterpartyUserId = (p.ChargerOwnerId == me) ? p.VehicleOwnerId : p.ChargerOwnerId,
                    MyRoleUserTypeId = (p.ChargerOwnerId == me) ? 1 : 2,
                    VehicleOwnerRating = p.VehicleOwnerRating,
                    ChargerOwnerRating = p.ChargerOwnerRating,
                    ChargerProtocolName = p.ChargerRequest != null && p.ChargerRequest.Charger != null && p.ChargerRequest.Charger.Protocol != null
                        ? p.ChargerRequest.Charger.Protocol.Name
                        : null,
                    ChargerCapacityKw = p.ChargerRequest != null && p.ChargerRequest.Charger != null && p.ChargerRequest.Charger.Capacity != null
                        ? (int?)p.ChargerRequest.Charger.Capacity.kw
                        : null
                });

            // Use default pagination if not provided
            paginationParams ??= new PaginationParams { PageNumber = 1, PageSize = 20 };

            var pagedResult = await _paginationService.PaginateAsync(query, paginationParams, ct);

            return new ApiResponse<object>(pagedResult, "My activities fetched", true);
        }

        private async Task SendToUserAsync(string userId, string title, string body, int relatedRequestId, string notificationType, CancellationToken ct, Dictionary<string, string>? extraData = null)
        {
            var tokens = await _ctx.Set<DeviceToken>()
                                   .AsNoTracking()
                                   .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Token))
                                   .Select(t => t.Token)
                                   .ToListAsync(ct);

            foreach (var tk in tokens)
            {
                try { await _firebase.SendNotificationAsync(tk, title, body, relatedRequestId, notificationType, extraData); }
                catch (Exception ex) { _logger.LogError(ex, "Failed to send notification to token {Token}", tk); }
            }
        }
        private string CurrentUserId()
           => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private async Task<Notification> AddNotificationAsync(
    string receiverUserId,
    int relatedRequestId,
    string title,
    string body,
    int userTypeId,
    string notificationType,
    CancellationToken ct)
        {
            var notification = new Notification
            {
                Title = title,
                Body = body,
                IsRead = false,
                SentAt = DateTimeHelper.GetEgyptTime(),
                UserId = receiverUserId,
                RelatedRequestId = relatedRequestId,
                UserTypeId = userTypeId,
                Type = notificationType
            };

            await _ctx.AddAsync(notification, ct);
            await _ctx.SaveChangesAsync(ct);
            return notification;
        }
        private async Task<NotificationResultDto> SendAndPersistNotificationAsync(
      string receiverUserId,
      int requestId,
      string title,
      int processId,
      string body,
      string notificationType,
      int userTypeId,
      CancellationToken ct,
      Dictionary<string, string>? extraData = null // NEW
  )
        {
            var data = new Dictionary<string, string>
            {
                ["NotificationType"] = notificationType,
                ["requestId"] = requestId.ToString(),
                ["processId"] = processId.ToString()
            };

            if (extraData != null)
                foreach (var kv in extraData) data[kv.Key] = kv.Value;

            var tokens = await _ctx.Set<DeviceToken>()
                                   .AsNoTracking()
                                   .Where(t => t.UserId == receiverUserId && !string.IsNullOrEmpty(t.Token))
                                   .Select(t => t.Token)
                                   .ToListAsync(ct);

            if (tokens.Count > 0)
            {
                await Task.WhenAll(tokens.Select(tk =>
                    _firebase.SendNotificationAsync(
                        tk, title, body, requestId, notificationType, data
                    )
                ));
            }

            var notification = await AddNotificationAsync(
                receiverUserId, requestId, title, body, userTypeId, notificationType, ct
            );

            // ⬅️ خزّن نسخة من الـ data داخل نتيجة الإشعار (اختياري لكنه عملي للديبج)
            return new NotificationResultDto(
                NotificationId: notification.Id,
                RequestId: requestId,
                RecipientUserId: receiverUserId,
                Title: title,
                Body: body,
                NotificationType: notificationType,
                SentAt: notification.SentAt,
                PushSentCount: tokens.Count,
                ExtraData: data // NEW
            );
        }

        public async Task<ApiResponse<PendingProcessDto>> GetPendingProcessesAsync(CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<PendingProcessDto>("Unauthorized", false);

            var pendingStatuses = new[] { "pending", "accepted", "confirmed", "Started", "PendingCompleted", "Completed" };

            var req = await _ctx.Set<ChargingRequestEntity>()
                .AsNoTracking()
                .Include(r => r.CarOwner)
                .Include(r => r.Charger).ThenInclude(c => c.User)
                .Include(r => r.Charger).ThenInclude(c => c.Protocol)
                .Include(r => r.Charger).ThenInclude(c => c.Capacity)
                .Where(r =>
                    (r.UserId == me || r.RecipientUserId == me) &&
                    pendingStatuses.Contains(r.Status))
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync(ct);

            if (req == null)
                return new ApiResponse<PendingProcessDto>(null, "No active process", true);

            var process = await _ctx.Set<ProcessEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChargerRequestId == req.Id, ct);

            // Skip fully completed requests (not in rating phase)
            if (req.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                && (process == null || process.SubStatus != "awaiting_rating"))
                return new ApiResponse<PendingProcessDto>(null, "No active process", true);

            // Auto-heal: process has both ratings but was never finalized
            if (process != null
                && process.VehicleOwnerRating.HasValue
                && process.ChargerOwnerRating.HasValue
                && process.Status != ProcessStatus.Completed
                && process.Status != ProcessStatus.Disputed)
            {
                var trackedProcess = await _ctx.Set<ProcessEntity>()
                    .FirstOrDefaultAsync(p => p.Id == process.Id, ct);
                if (trackedProcess != null)
                {
                    var hasReport = await _ctx.Set<UserReport>()
                        .AnyAsync(r => r.ProcessId == process.Id, ct);

                    trackedProcess.Status = hasReport ? ProcessStatus.Disputed : ProcessStatus.Completed;
                    trackedProcess.SubStatus = null;
                    if (trackedProcess.DateCompleted == null)
                        trackedProcess.DateCompleted = DateTimeHelper.GetEgyptTime();

                    var trackedReq = await _ctx.Set<ChargingRequestEntity>()
                        .FirstOrDefaultAsync(r => r.Id == req.Id, ct);
                    if (trackedReq != null)
                        trackedReq.Status = hasReport ? "Disputed" : "Completed";

                    await _ctx.SaveChangesAsync(ct);

                    _logger.LogWarning(
                        "Auto-healed stuck process {ProcessId}: both ratings exist, forced to {Status}",
                        process.Id, trackedProcess.Status);
                }
                return new ApiResponse<PendingProcessDto>(null, "No active process", true);
            }

            var isVehicleOwner = req.UserId == me;
            var isChargerOwner = req.RecipientUserId == me;

            var (computedSubStatus, screenKey, notificationType, availableActions) =
                DetermineStatusContext(req.Status, isVehicleOwner, isChargerOwner);

            // SubStatus drives notification type ONLY for started/pendingcompleted states.
            // "confirmed" is PROTECTED — SubStatus must NEVER override it.
            var reqStatusLower = req.Status.ToLower();
            if (process?.SubStatus != null && reqStatusLower != "confirmed")
            {
                notificationType = process.SubStatus switch
                {
                    "process_updated" when reqStatusLower is "started" or "pendingcompleted"
                        => NotificationTypes.VehicleOwner_UpdateProcess,
                    "awaiting_rating"
                        => NotificationTypes.ChargerOwner_ConfirmedProcessSuccessfully,
                    _ => notificationType
                };
            }

            var subStatus = process?.SubStatus ?? computedSubStatus;

            if (process?.SubStatus != null)
                screenKey = DeriveScreenKeyFromSubStatus(process.SubStatus, isVehicleOwner, isChargerOwner);

            if (process != null)
            {
                var expectedSubStatus = GetExpectedSubStatus(req.Status);
                if (expectedSubStatus != null && process.SubStatus != expectedSubStatus)
                {
                    _logger.LogWarning(
                        "SubStatus mismatch detected: ProcessId={ProcessId}, ChargingRequest.Status={Status}, " +
                        "Process.SubStatus={SubStatus}, Expected={Expected}",
                        process.Id, req.Status, process.SubStatus ?? "(null)", expectedSubStatus);
                }
            }

            var uiContext = BuildUiContext(req, process, notificationType);

            var dto = new PendingProcessDto
            {
                ProcessId = process?.Id,
                RequestId = req.Id,
                Status = req.Status,
                SubStatus = subStatus,
                UserRole = isVehicleOwner ? "vehicle_owner" : "charger_owner",
                UiContext = uiContext,
                Resume = new ResumeContext
                {
                    ScreenKey = screenKey,
                    Params = BuildResumeParams(req.Id, process?.Id)
                },
                CreatedAt = req.RequestedAt
            };

            return new ApiResponse<PendingProcessDto>(dto, "Pending process fetched", true);
        }

        /// <summary>
        /// Builds uiContext that EXACTLY mirrors the FCM notification data payload.
        /// Keys and string formatting match what is sent via push notifications.
        /// </summary>
        private static PendingProcessUiContext BuildUiContext(ChargingRequestEntity req, ProcessEntity? process, string notificationType)
        {
            var statusLower = req.Status.ToLower();
            var uiContext = new PendingProcessUiContext
            {
                // Base fields (always present in FCM data)
                RequestId = req.Id.ToString(),
                NotificationType = notificationType
            };

            // Timer fields - only for accepted (mirrors ChargerOwner_AcceptRequest FCM payload)
            // and confirmed (mirrors ChargerOwner_ConfirmedProcessSuccessfully SignalR payload)
            if (statusLower == "accepted")
            {
                if (req.RespondedAt.HasValue)
                    uiContext.TimerStartedAt = req.RespondedAt.Value.ToString("o");
                uiContext.TimerDurationMinutes = "10";
            }
            else if (statusLower == "confirmed")
            {
                if (req.ConfirmedAt.HasValue)
                    uiContext.TimerStartedAt = req.ConfirmedAt.Value.ToString("o");
                uiContext.TimerDurationMinutes = "15";
            }

            // Process/payment fields - for PendingCompleted, Started (mirrors VehicleOwner_CreateProcess / VehicleOwner_UpdateProcess)
            if (process != null && (statusLower == "pendingcompleted" || statusLower == "started" || statusLower == "completed"))
            {
                uiContext.ProcessId = process.Id.ToString();
                uiContext.EstimatedPrice = (process.EstimatedPrice ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                uiContext.AmountCharged = (process.AmountCharged ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                uiContext.AmountPaid = (process.AmountPaid ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            }

            return uiContext;
        }

        private static (string subStatus, string screenKey, string notificationType, List<string> availableActions)
            DetermineStatusContext(string status, bool isVehicleOwner, bool isChargerOwner)
        {
            var statusLower = status.ToLower();

            // notificationType = the REAL type sent by the notification system (same for both roles)
            // screenKey + availableActions = role-specific (different actions per user)
            return statusLower switch
            {
                "pending" when isChargerOwner => (
                    "awaiting_response",
                    "INCOMING_REQUEST",
                    NotificationTypes.VehicleOwner_RequestCharger,
                    new List<string> { "accept", "reject" }
                ),
                "pending" when isVehicleOwner => (
                    "awaiting_response",
                    "WAITING_FOR_RESPONSE",
                    NotificationTypes.VehicleOwner_RequestCharger,
                    new List<string> { "cancel" }
                ),
                "accepted" when isVehicleOwner => (
                    "request_accepted",
                    "CONFIRM_REQUEST",
                    NotificationTypes.ChargerOwner_AcceptRequest,
                    new List<string> { "confirm", "abort" }
                ),
                "accepted" when isChargerOwner => (
                    "request_accepted",
                    "WAITING_FOR_VEHICLE",
                    NotificationTypes.ChargerOwner_AcceptRequest,
                    new List<string> { "abort" }
                ),
                "confirmed" when isChargerOwner => (
                    "charging_confirmed",
                    "START_CHARGING",
                    NotificationTypes.ChargerOwner_ConfirmedProcessSuccessfully,
                    new List<string> { "start", "abort" }
                ),
                "confirmed" when isVehicleOwner => (
                    "charging_confirmed",
                    "WAITING_FOR_START",
                    NotificationTypes.ChargerOwner_ConfirmedProcessSuccessfully,
                    new List<string> { "abort" }
                ),
                "started" when isVehicleOwner => (
                    "charging_in_progress",
                    "CHARGING_ACTIVE",
                    NotificationTypes.VehicleOwner_CreateProcess,
                    new List<string> { "abort" }
                ),
                "started" when isChargerOwner => (
                    "charging_in_progress",
                    "CHARGING_ACTIVE",
                    NotificationTypes.VehicleOwner_CreateProcess,
                    new List<string> { "abort" }
                ),
                "pendingcompleted" when isChargerOwner => (
                    "awaiting_completion",
                    "CONFIRM_COMPLETION",
                    NotificationTypes.VehicleOwner_CreateProcess,
                    new List<string> { "complete", "report" }
                ),
                "pendingcompleted" when isVehicleOwner => (
                    "awaiting_completion",
                    "WAITING_FOR_COMPLETION",
                    NotificationTypes.VehicleOwner_CreateProcess,
                    new List<string> { "report" }
                ),
                "completed" when isChargerOwner => (
                    "awaiting_rating",
                    "RATING_SCREEN",
                    NotificationTypes.ChargerOwner_ConfirmedProcessSuccessfully,
                    new List<string> { "rate" }
                ),
                "completed" when isVehicleOwner => (
                    "awaiting_rating",
                    "RATING_SCREEN",
                    NotificationTypes.ChargerOwner_ConfirmedProcessSuccessfully,
                    new List<string> { "rate" }
                ),
                _ => (
                    "unknown",
                    "UNKNOWN",
                    "Unknown",
                    new List<string>()
                )
            };
        }

        /// <summary>
        /// Derives ScreenKey from persisted SubStatus and user role.
        /// Used when SubStatus is read from DB.
        /// </summary>
        private static string DeriveScreenKeyFromSubStatus(string subStatus, bool isVehicleOwner, bool isChargerOwner)
        {
            return subStatus switch
            {
                "awaiting_completion" when isChargerOwner => "CONFIRM_COMPLETION",
                "awaiting_completion" when isVehicleOwner => "WAITING_FOR_COMPLETION",
                "charging_in_progress" => "CHARGING_ACTIVE",
                "process_updated" => "CHARGING_ACTIVE",
                "awaiting_rating" => "RATING_SCREEN",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Returns the expected SubStatus for a given ChargingRequest status.
        /// Used for consistency checking. Returns null for states where SubStatus is not expected.
        /// </summary>
        private static string? GetExpectedSubStatus(string chargingRequestStatus)
        {
            return chargingRequestStatus.ToLower() switch
            {
                "pendingcompleted" => "awaiting_completion",
                // Started can be "charging_in_progress" or "process_updated", skip consistency check
                "started" => null,
                // Final states (Completed, Aborted) and pre-process states don't have expected SubStatus
                _ => null
            };
        }

        private static Dictionary<string, object> BuildResumeParams(int requestId, int? processId)
        {
            var p = new Dictionary<string, object>
            {
                ["requestId"] = requestId
            };
            if (processId.HasValue)
                p["processId"] = processId.Value;
            return p;
        }

        /// <summary>
        /// Unified termination path for all process exits.
        /// - Idempotent: safe to call even if process already terminal
        /// - Sets Process.Status and ChargingRequest.Status
        /// - Cleans up CurrentActivities for both users
        /// - Resets IsAvailable if no other activities
        /// - Sends Process_Terminated notification to both users
        /// </summary>
        public async Task TerminateProcessAsync(
            int processId,
            ProcessStatus targetStatus,
            string terminationReason,
            string? actorUserId = null,
            CancellationToken ct = default)
        {
            var process = await _ctx.Set<ProcessEntity>()
                .FirstOrDefaultAsync(p => p.Id == processId, ct);

            if (process == null)
            {
                _logger.LogWarning("TerminateProcessAsync: Process {ProcessId} not found", processId);
                return;
            }

            // Idempotency: if already terminal, just ensure cleanup
            var isAlreadyTerminal = process.Status == ProcessStatus.Completed
                                 || process.Status == ProcessStatus.Aborted
                                 || process.Status == ProcessStatus.Disputed;

            if (!isAlreadyTerminal)
            {
                process.Status = targetStatus;
                process.DateCompleted = DateTimeHelper.GetEgyptTime();
                _ctx.Entry(process).State = EntityState.Modified;
            }

            // Update ChargingRequest status
            var request = await _ctx.Set<ChargingRequestEntity>()
                .FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);

            if (request != null)
            {
                var reqStatus = targetStatus switch
                {
                    ProcessStatus.Completed => "Completed",
                    ProcessStatus.Disputed => "Disputed",
                    _ => "Aborted"
                };
                if (request.Status != reqStatus && !isAlreadyTerminal)
                {
                    request.Status = reqStatus;
                    _ctx.Entry(request).State = EntityState.Modified;
                }
            }

            // Cleanup users + send notifications
            foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
            {
                var user = await _ctx.Set<AppUser>()
                    .Include(u => u.DeviceTokens)
                    .FirstOrDefaultAsync(u => u.Id == uid, ct);

                if (user != null)
                {
                    // Remove from CurrentActivities
                    var list = user.CurrentActivities.ToList();
                    if (list.Contains(processId))
                    {
                        list.Remove(processId);
                        user.CurrentActivities = list;
                        _ctx.Entry(user).Property(u => u.CurrentActivitiesJson).IsModified = true;
                    }

                    // Reset availability if no more active processes
                    if (user.CurrentActivities.Count == 0 && !user.IsAvailable)
                    {
                        user.IsAvailable = true;
                        _ctx.Entry(user).Property(u => u.IsAvailable).IsModified = true;
                    }

                    // Send Process_Terminated notification via FCM (only if not already terminal)
                    if (!isAlreadyTerminal && user.DeviceTokens?.Any() == true)
                    {
                        var extraData = new Dictionary<string, string>
                        {
                            ["processId"] = processId.ToString(),
                            ["requestId"] = process.ChargerRequestId.ToString(),
                            ["terminationReason"] = terminationReason,
                            ["terminatedAt"] = DateTime.UtcNow.ToString("o"),
                            ["userRole"] = uid == process.VehicleOwnerId ? "vehicle_owner" : "charger_owner"
                        };

                        foreach (var token in user.DeviceTokens)
                        {
                            try
                            {
                                await _firebase.SendNotificationAsync(
                                    token.Token,
                                    "Process terminated",
                                    GetTerminationMessage(terminationReason),
                                    process.ChargerRequestId,
                                    NotificationTypes.Process_Terminated,
                                    extraData);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to send termination notification to token");
                            }
                        }
                    }
                }
            }

            // Note: Caller should call SaveChangesAsync after this method
        }

        private static string GetTerminationMessage(string reason) => reason switch
        {
            "aborted" => "Charging process has been cancelled",
            "report" => "Process terminated due to a report",
            "timeout" => "Process terminated",
            "expired" => "Process has expired",
            "rejected" => "Request rejected",
            _ => "Process terminated"
        };

    }
}
