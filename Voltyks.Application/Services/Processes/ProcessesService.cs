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
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Application.Utilities;
using Voltyks.Core.DTOs.ChargerRequest;
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

        public ProcessesService(VoltyksDbContext ctx, IHttpContextAccessor http, IFirebaseService firebase, ILogger<ProcessesService> logger, IRedisService redisService)
        {
            _ctx = ctx; _http = http;
            _firebase = firebase;
            _logger = logger;
            _redisService = redisService;
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
                Status = ProcessStatus.PendingCompleted
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
                    _ctx.Update(vo);
                }

                if (co != null)
                {
                    var list = co.CurrentActivities.ToList();
                    if (!list.Contains(process.Id))
                    {
                        list.Add(process.Id);
                        co.CurrentActivities = list;
                    }
                    _ctx.Update(co);
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
                    ["amountPaid"] = (process.AmountPaid ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
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

                // حالة العملية
                if (decision == "completed")
                {
                    process.Status = ProcessStatus.Completed;
                    process.DateCompleted = DateTimeHelper.GetEgyptTime();
                    request.Status = "Completed";
                }
                else if (decision == "started")
                {
                    request.Status = "Started";
                }
                else if (decision == "aborted" || decision == "ended-by-report")
                {
                    process.Status = ProcessStatus.Aborted;
                    request.Status = "Aborted";
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
                    ["amountPaid"] = (process.AmountPaid ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                };

                var notifDto = await SendAndPersistNotificationAsync(
                    receiverUserId: process.ChargerOwnerId,
                    requestId: process.ChargerRequestId,
                    processId: process.Id,
                    title: title,
                    body: body,
                    notificationType: NotificationTypes.VehicleOwner_UpdateProcess,
                    userTypeId: 1,
                    ct: ct,
                    extraData: extraData
                );

                await tx.CommitAsync(ct);

                // نفس شكل create في الـ response
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

            // ⚙️ تطبيع القرار على القيم الجديدة
            var raw = (dto.Decision ?? "Process-Completed").Trim();
            // نقارن Case-Insensitive
            var decision = raw.Equals("Process-Completed", StringComparison.OrdinalIgnoreCase) ? "completed"
                         : raw.Equals("Process-Ended-By-Report", StringComparison.OrdinalIgnoreCase) ? "ended-by-report"
                         : raw.Equals("Process-Started", StringComparison.OrdinalIgnoreCase) ? "started"
                         : raw.Equals("Process-Aborted", StringComparison.OrdinalIgnoreCase) ? "aborted"
                         : "completed"; // الافتراضي

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                if (decision == "completed")
                {
                    process.Status = ProcessStatus.Completed;
                    if (process.DateCompleted == null)
                        process.DateCompleted = DateTimeHelper.GetEgyptTime();
                    request.Status = "Completed";

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
                            ct
                        );
                    }
                    else // Vehicle Owner
                    {
                        await SendToUserAsync(
                            process.ChargerOwnerId,
                            "Process confirmed",
                            "Vehicle owner confirmed the session completion.",
                            request.Id,
                            "VehicleOwner_ConfirmProcess",
                            ct
                        );
                    }
                }
                else if (decision == "started")
                {
                    // بدء العملية: بنعلّم الطلب إنها بدأت
                    // لو عندك ProcessStatus.Started استخدمه؛ غير كده هنسيب Status زي ما هو ونعلم الطلب
                    request.Status = "Started";
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
                        ct
                    );
                }
                else // ended-by-report | aborted  -> نفس مسار الإنهاء/التبليغ
                {
                    process.Status = ProcessStatus.Aborted;
                    request.Status = "Aborted";

                    _ctx.Update(process);
                    _ctx.Update(request);
                    await _ctx.SaveChangesAsync(ct);

                    if (isChargerOwner)
                    {
                        await SendToUserAsync(
                            process.VehicleOwnerId,
                            "Process reported",
                            "Charger owner reported/aborted this session.",
                            request.Id,
                            "ChargerOwner_ReportProcess",
                            ct
                        );
                    }
                    else // Vehicle Owner
                    {
                        await SendToUserAsync(
                            process.ChargerOwnerId,
                            "Process reported",
                            "Vehicle owner reported/aborted this session.",
                            request.Id,
                            "VehicleOwner_ReportProcess",
                            ct
                        );
                    }
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
                process.Status = ProcessStatus.Completed;
                process.DateCompleted = DateTimeHelper.GetEgyptTime();

                // (اختياري) شيلها من CurrentActivities
                foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                {
                    var u = await _ctx.Set<AppUser>().FindAsync(new object?[] { uid }, ct);
                    if (u != null)
                    {
                        var list = u.CurrentActivities.ToList();
                        if (list.Contains(process.Id))
                        {
                            list.Remove(process.Id);
                            u.CurrentActivities = list;
                            _ctx.Update(u);
                        }
                    }
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

            var ratingNotifDto = await SendAndPersistNotificationAsync(
                receiverUserId: receiverUserId,
                requestId: relatedRequestId,
                processId: process.Id,
                title: title,
                body: body,
                notificationType: notificationType,
                userTypeId: userTypeId,
                ct: ct
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


        public async Task<ApiResponse<object>> GetMyActivitiesAsync(CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var items = await _ctx.Set<ProcessEntity>()
                .AsNoTracking()
                .Where(p => p.VehicleOwnerId == me || p.ChargerOwnerId == me)
                .OrderByDescending(p => p.DateCreated)
                .Take(50)
                .Select(p => new
                {
                    // الحقول الأصلية
                    p.Id,
                    p.ChargerRequestId,
                    p.Status,
                    p.AmountCharged,
                    p.AmountPaid,
                    p.DateCreated,
                    p.DateCompleted,

                    // ✅ تمييز دوري في العملية
                    // true لو أنا صاحب الشاحن في هذه العملية
                    IsAsChargerOwner = (p.ChargerOwnerId == me),
                    // true لو أنا صاحب العربية في هذه العملية
                    IsAsVehicleOwner = (p.VehicleOwnerId == me),

                    // ✅ اتجاه النشاط من منظوري:
                    // Incoming: جايالي طلب/تفاعل (أنا ChargerOwner)
                    // Outgoing: أنا اللي بادرت (أنا VehicleOwner)
                    Direction = (p.ChargerOwnerId == me) ? "Incoming" : "Outgoing",

                    // ✅ معلومات الطرف الآخر (اختياري: الاسم)
                    CounterpartyUserId = (p.ChargerOwnerId == me) ? p.VehicleOwnerId : p.ChargerOwnerId,
                    //CounterpartyName = _ctx.Set<AppUser>()
                    //                       .Where(u => u.Id == ((p.ChargerOwnerId == me) ? p.VehicleOwnerId : p.ChargerOwnerId))
                    //                       .Select(u => u.FullName)
                    //                       .FirstOrDefault(),

                    // ✅ نوع المستخدم المستهدَف لو هتستخدمه في UI/Badges
                    // 1 = ChargerOwner, 2 = VehicleOwner (لو حابب تلتزم بثوابتك)
                    MyRoleUserTypeId = (p.ChargerOwnerId == me) ? 1 : 2,

                    // ✅ التقييمات
                    p.VehicleOwnerRating,
                    p.ChargerOwnerRating,

                    // ✅ نوع الشاحن
                    ChargerProtocolName = p.ChargerRequest != null && p.ChargerRequest.Charger != null && p.ChargerRequest.Charger.Protocol != null
                        ? p.ChargerRequest.Charger.Protocol.Name
                        : null,
                    ChargerCapacityKw = p.ChargerRequest != null && p.ChargerRequest.Charger != null && p.ChargerRequest.Charger.Capacity != null
                        ? (int?)p.ChargerRequest.Charger.Capacity.kw
                        : null
                })
                .ToListAsync(ct);

            return new ApiResponse<object>(items, "My activities fetched", true);
        }

        private async Task SendToUserAsync(string userId, string title, string body, int relatedRequestId, string notificationType, CancellationToken ct)
        {
            var tokens = await _ctx.Set<DeviceToken>()
                                   .AsNoTracking()
                                   .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Token))
                                   .Select(t => t.Token)
                                   .ToListAsync(ct);

            foreach (var tk in tokens)
            {
                try { await _firebase.SendNotificationAsync(tk, title, body, relatedRequestId, notificationType); }
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
                UserTypeId = userTypeId
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
                receiverUserId, requestId, title, body, userTypeId, ct
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




    }
}
