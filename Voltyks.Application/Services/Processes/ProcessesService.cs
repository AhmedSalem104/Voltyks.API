using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.ChargingRequest;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Core.DTOs.Process;
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

        public ProcessesService(VoltyksDbContext ctx, IHttpContextAccessor http, IFirebaseService firebase)
        {
            _ctx = ctx; _http = http; 
            _firebase = firebase;
        }


        //public async Task<ApiResponse<object>> ConfirmByVehicleOwnerAsync(ConfirmByVehicleOwnerDto dto, CancellationToken ct = default)
        //{
        //    var me = CurrentUserId();
        //    if (string.IsNullOrEmpty(me))
        //        return new ApiResponse<object>("Unauthorized", false);

        //    var req = await _ctx.Set<ChargingRequestEntity>()
        //        .FirstOrDefaultAsync(r => r.Id == dto.ChargerRequestId, ct);
        //    if (req is null) return new ApiResponse<object>("Charger request not found", false);

        //    // تأكد إن المنفّذ هو صاحب العربية
        //    if (req.UserId != me) return new ApiResponse<object>("Forbidden", false);

        //    // لو已有 Process لنفس الطلب امنع التكرار
        //    var exists = await _ctx.Set<ProcessEntity>().AnyAsync(p => p.ChargerRequestId == req.Id, ct);
        //    if (exists) return new ApiResponse<object>("Process already created for this request", false);

        //    // إنشاء Process + تحديث حالة الطلب
        //    var process = new ProcessEntity
        //    {
        //        ChargerRequestId = req.Id,
        //        VehicleOwnerId = req.UserId,
        //        ChargerOwnerId = req.RecipientUserId!,
        //        EstimatedPrice = dto.EstimatedPrice,
        //        AmountCharged = dto.AmountCharged,
        //        AmountPaid = dto.AmountPaid,
        //        Status = ProcessStatus.PendingCompleted
        //    };

        //    using var tx = await _ctx.Database.BeginTransactionAsync(ct);
        //    try
        //    {
        //        await _ctx.AddAsync(process, ct);

        //        req.Status = "PendingCompleted";
        //        _ctx.Update(req);

        //        // (اختياري) أضف الـ processId إلى CurrentActivities بعد الحفظ
        //        await _ctx.SaveChangesAsync(ct);

        //        // بعد SaveChanges الأول (اللي بيولّد process.Id)
        //        var vo = await _ctx.Set<AppUser>().FindAsync(new object?[] { req.UserId }, ct);
        //        var co = await _ctx.Set<AppUser>().FindAsync(new object?[] { req.RecipientUserId }, ct);

        //        if (vo != null)
        //        {
        //            var list = vo.CurrentActivities.ToList();
        //            if (!list.Contains(process.Id)) { list.Add(process.Id); vo.CurrentActivities = list; }
        //            _ctx.Update(vo);
        //        }
        //        if (co != null)
        //        {
        //            var list = co.CurrentActivities.ToList();
        //            if (!list.Contains(process.Id)) { list.Add(process.Id); co.CurrentActivities = list; }
        //            _ctx.Update(co);
        //        }
        //        await _ctx.SaveChangesAsync(ct);

        //        await tx.CommitAsync(ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        await tx.RollbackAsync(ct);
        //        return new ApiResponse<object>("Failed to start process", false, new() { ex.Message });
        //    }

        //    // (اختياري) Notification → لصاحب المحطة بالقيم

        //    return new ApiResponse<object>(new { processId = process.Id }, "Process created & request moved to PendingCompleted", true);
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

            var exists = await _ctx.Set<ProcessEntity>().AnyAsync(p => p.ChargerRequestId == req.Id, ct);
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
                    if (!list.Contains(process.Id)) { list.Add(process.Id); vo.CurrentActivities = list; }
                    _ctx.Update(vo);
                }
                if (co != null)
                {
                    var list = co.CurrentActivities.ToList();
                    if (!list.Contains(process.Id)) { list.Add(process.Id); co.CurrentActivities = list; }
                    _ctx.Update(co);
                }
                await _ctx.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>("Failed to start process", false, new() { ex.Message });
            }

            // 🔔 إشعار لصاحب المحطة بالتفاصيل
            var title = "Process confirmation pending";
            var body = $"Amount Charged: {dto.AmountCharged:0.##} | Amount Paid: {dto.AmountPaid:0.##}";
            await SendToUserAsync(process.ChargerOwnerId, title, body, req.Id, "VehicleOwner_ConfirmProcess", ct);

            return new ApiResponse<object>(new { processId = process.Id }, "Process created & request moved to PendingCompleted", true);
        }

        //public async Task<ApiResponse<object>> OwnerDecisionAsync(OwnerDecisionDto dto, CancellationToken ct = default)
        //{
        //    var me = CurrentUserId();
        //    if (string.IsNullOrEmpty(me))
        //        return new ApiResponse<object>("Unauthorized", false);

        //    var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
        //    if (process is null) return new ApiResponse<object>("Process not found", false);

        //    // المصرّح: صاحب المحطة فقط
        //    if (process.ChargerOwnerId != me) return new ApiResponse<object>("Forbidden", false);

        //    var request = await _ctx.Set<ChargingRequestEntity>().FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
        //    if (request is null) return new ApiResponse<object>("Charger request not found", false);

        //    var decision = (dto.Decision ?? "Confirm").Trim().ToLowerInvariant();

        //    using var tx = await _ctx.Database.BeginTransactionAsync(ct);
        //    try
        //    {
        //        if (decision == "confirm")
        //        {
        //            process.Status = ProcessStatus.Completed; // مرحليًا قبل التقييم
        //            process.DateCompleted = GetEgyptTime();
        //            request.Status = "Completed";

        //            _ctx.Update(process);
        //            _ctx.Update(request);
        //            await _ctx.SaveChangesAsync(ct);

        //            // (اختياري) سوي الرسوم تلقائيًا
        //            // await _feesService.TransferVoltyksFeesAsync(new RequestIdDto { RequestId = process.ChargerRequestId }, ct);
        //        }
        //        else
        //        {
        //            process.Status = ProcessStatus.Aborted;
        //            request.Status = "Aborted";

        //            _ctx.Update(process);
        //            _ctx.Update(request);
        //            await _ctx.SaveChangesAsync(ct);

        //            // ممكن هنا تعمل Rollback لأي آثار أخرى لو عندك
        //        }

        //        await tx.CommitAsync(ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        await tx.RollbackAsync(ct);
        //        return new ApiResponse<object>("Failed to apply decision", false, new() { ex.Message });
        //    }

        //    // (اختياري) Notifications للطرفين

        //    var msg = decision == "confirm" ? "Process confirmed" : "Process reported (aborted)";
        //    return new ApiResponse<object>(new { processId = process.Id, status = process.Status.ToString() }, msg, true);
        //}
        public async Task<ApiResponse<object>> OwnerDecisionAsync(OwnerDecisionDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
            if (process is null) return new ApiResponse<object>("Process not found", false);

            if (process.ChargerOwnerId != me) return new ApiResponse<object>("Forbidden", false);

            var request = await _ctx.Set<ChargingRequestEntity>().FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
            if (request is null) return new ApiResponse<object>("Charger request not found", false);

            var decision = (dto.Decision ?? "Confirm").Trim().ToLowerInvariant();

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                if (decision == "confirm")
                {
                    process.Status = ProcessStatus.Completed; // قبل التقييم
                    process.DateCompleted = GetEgyptTime();
                    request.Status = "Completed";

                    _ctx.Update(process);
                    _ctx.Update(request);
                    await _ctx.SaveChangesAsync(ct);

                    // 🔔 إشعار لصاحب العربية بالموافقة
                    await SendToUserAsync(process.VehicleOwnerId,
                        "Process confirmed",
                        "Charger owner confirmed your session. Please submit your rating.",
                        request.Id,
                        "ChargerOwner_ConfirmProcess", ct);
                }
                else
                {
                    process.Status = ProcessStatus.Aborted;
                    request.Status = "Aborted";

                    _ctx.Update(process);
                    _ctx.Update(request);
                    await _ctx.SaveChangesAsync(ct);

                    // 🔔 إشعار لصاحب العربية بالإبلاغ/الإلغاء
                    await SendToUserAsync(process.VehicleOwnerId,
                        "Process reported",
                        "Charger owner reported/aborted this session.",
                        request.Id,
                        "ChargerOwner_ReportProcess", ct);
                }

                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>("Failed to apply decision", false, new() { ex.Message });
            }

            var msg = decision == "confirm" ? "Process confirmed" : "Process reported (aborted)";
            return new ApiResponse<object>(new { processId = process.Id, status = process.Status.ToString() }, msg, true);
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

        //    // حدّد من يقيم مين (زي ما عندك)
        //    var raterId = me;
        //    var rateeId = (process.VehicleOwnerId == me) ? process.ChargerOwnerId
        //               : (process.ChargerOwnerId == me) ? process.VehicleOwnerId
        //               : null;
        //    if (rateeId is null) return new ApiResponse<object>("Forbidden", false);

        //    // 1) امنع تكرار التقييم لنفس العملية من نفس الشخص
        //    var already = await _ctx.Set<RatingsHistory>()
        //        .AnyAsync(x => x.ProcessId == process.Id && x.RaterUserId == raterId, ct);
        //    if (already) return new ApiResponse<object>("You already rated this process", false);

        //    // 2) خزّن التقييم في Process (per-process rating)
        //    if (me == process.VehicleOwnerId) process.ChargerOwnerRating = dto.RatingForOther;
        //    else process.VehicleOwnerRating = dto.RatingForOther;

        //    // 3) خزّله في RatingsHistory
        //    await _ctx.AddAsync(new RatingsHistory
        //    {
        //        ProcessId = process.Id,
        //        RaterUserId = raterId,
        //        RateeUserId = rateeId!,
        //        Stars = dto.RatingForOther
        //    }, ct);

        //    // 4) حدّث التقييم العام للمستخدم المُقَيّم
        //    var ratee = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == rateeId, ct);
        //    ratee!.Rating = ((ratee.Rating * ratee.RatingCount) + dto.RatingForOther) / (ratee.RatingCount + 1);
        //    ratee.RatingCount += 1;

        //    // 5) لو الاتنين قيّموا → كمّل الحالة نهائيًا
        //    if (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue)
        //    {
        //        process.Status = ProcessStatus.Completed;
        //        process.DateCompleted = GetEgyptTime();

        //        // (اختياري) لو عايز تشيلها من MyActivities:
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


        //    return new ApiResponse<object>(new
        //    {
        //        processId = process.Id,
        //        processStatus = process.Status.ToString(),
        //        yourRatingForOther = dto.RatingForOther
        //    }, "Rating submitted", true);
        //}
        //public async Task<ApiResponse<object>> SubmitRatingAsync(SubmitRatingDto dto, CancellationToken ct = default)
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

            // تحديث المتوسط العام للمستخدم المُقَيَّم
            var ratee = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == rateeId, ct);
            ratee!.Rating = ((ratee.Rating * ratee.RatingCount) + dto.RatingForOther) / (ratee.RatingCount + 1);
            ratee.RatingCount += 1;

            // لو الاتنين قيّموا، أنهِ العملية
            if (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue)
            {
                process.Status = ProcessStatus.Completed;
                process.DateCompleted = GetEgyptTime();

                // (اختياري) شيلها من CurrentActivities
                foreach (var uid in new[] { process.VehicleOwnerId, process.ChargerOwnerId })
                {
                    var u = await _ctx.Set<AppUser>().FindAsync(new object?[] { uid }, ct);
                    if (u != null)
                    {
                        var list = u.CurrentActivities.ToList();
                        if (list.Contains(process.Id)) { list.Remove(process.Id); u.CurrentActivities = list; _ctx.Update(u); }
                    }
                }
            }

            await _ctx.SaveChangesAsync(ct);

            // ⬇️ ارجع التقييمين من جدول Process نفسه
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
                otherRatingForYou = process.ChargerOwnerRating;   // تقييم VO ليك (CO)
            }

            return new ApiResponse<object>(new
            {
                processId = process.Id,
                processStatus = process.Status.ToString(),
                yourRatingForOther,
                otherRatingForYou // ممكن تكون null لو الطرف الآخر لسه ما قيّمش
            }, "Rating submitted", true);
        }

        //{
        //    var me = CurrentUserId();
        //    if (string.IsNullOrEmpty(me))
        //        return new ApiResponse<object>("Unauthorized", false);

        //    var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
        //    if (process is null) return new ApiResponse<object>("Process not found", false);

        //    if (dto.RatingForOther < 1 || dto.RatingForOther > 5)
        //        return new ApiResponse<object>("Invalid rating value (1..5)", false);

        //    // من يقيم مين؟
        //    var raterId = me;
        //    var rateeId = (process.VehicleOwnerId == me) ? process.ChargerOwnerId
        //               : (process.ChargerOwnerId == me) ? process.VehicleOwnerId
        //               : null;
        //    if (rateeId is null) return new ApiResponse<object>("Forbidden", false);

        //    // امنع تكرار التقييم
        //    var already = await _ctx.Set<RatingsHistory>()
        //        .AnyAsync(x => x.ProcessId == process.Id && x.RaterUserId == raterId, ct);
        //    if (already) return new ApiResponse<object>("You already rated this process", false);

        //    // خزّن في الـ Process
        //    if (me == process.VehicleOwnerId) process.ChargerOwnerRating = dto.RatingForOther;
        //    else process.VehicleOwnerRating = dto.RatingForOther;

        //    // history
        //    await _ctx.AddAsync(new RatingsHistory
        //    {
        //        ProcessId = process.Id,
        //        RaterUserId = raterId,
        //        RateeUserId = rateeId!,
        //        Stars = dto.RatingForOther
        //    }, ct);

        //    // حدّث المتوسط العام للمستخدم المُقَيَّم
        //    var ratee = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == rateeId, ct);
        //    ratee!.Rating = ((ratee.Rating * ratee.RatingCount) + dto.RatingForOther) / (ratee.RatingCount + 1);
        //    ratee.RatingCount += 1;

        //    // لو الاتنين قيّموا خلّص العملية
        //    if (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue)
        //    {
        //        process.Status = ProcessStatus.Completed;
        //        process.DateCompleted = GetEgyptTime();

        //        // اختياري: إزالة من CurrentActivities
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

        //    // ▼ جِب تقييم الطرف الآخر لك (لو موجود)
        //    var otherRatingForYou = await _ctx.Set<RatingsHistory>()
        //        .Where(x => x.ProcessId == process.Id && x.RaterUserId == rateeId && x.RateeUserId == raterId)
        //        .Select(x => (int?)x.Stars)
        //        .FirstOrDefaultAsync(ct);

        //    return new ApiResponse<object>(new
        //    {
        //        processId = process.Id,
        //        processStatus = process.Status.ToString(),
        //        yourRatingForOther = dto.RatingForOther,
        //        otherRatingForYou // ممكن تكون null لو لسه ما قيّمش
        //    }, "Rating submitted", true);
        //}
        //public async Task<ApiResponse<object>> GetRatingsSummaryAsync(int processId, CancellationToken ct = default)
        //{
        //    var me = CurrentUserId();
        //    if (string.IsNullOrEmpty(me))
        //        return new ApiResponse<object>("Unauthorized", false);

        //    var rh = _ctx.Set<RatingsHistory>()
        //                 .AsNoTracking()
        //                 .Where(x => x.ProcessId == processId);

        //    var yourForOther = await rh.Where(x => x.RaterUserId == me)
        //                               .Select(x => (int?)x.Stars)
        //                               .FirstOrDefaultAsync(ct);

        //    var p = await _ctx.Set<ProcessEntity>()
        //                      .AsNoTracking()
        //                      .FirstOrDefaultAsync(x => x.Id == processId, ct);

        //    if (p is null)
        //        return new ApiResponse<object>(new { yourRatingForOther = yourForOther, otherRatingForYou = (int?)null },
        //                                       "No process", true);

        //    var otherUser = (p.VehicleOwnerId == me) ? p.ChargerOwnerId
        //                 : (p.ChargerOwnerId == me) ? p.VehicleOwnerId
        //                 : null;

        //    int? otherForYou = null;
        //    if (otherUser != null)
        //    {
        //        otherForYou = await rh.Where(x => x.RaterUserId == otherUser && x.RateeUserId == me)
        //                              .Select(x => (int?)x.Stars)
        //                              .FirstOrDefaultAsync(ct);
        //    }

        //    return new ApiResponse<object>(new
        //    {
        //        yourRatingForOther = yourForOther,
        //        otherRatingForYou = otherForYou
        //    }, "Ratings summary", true);
        //}
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

            // Privacy: لازم يكون المستخدم طرف في العملية
            if (p.VehicleOwnerId != me && p.ChargerOwnerId != me)
                return new ApiResponse<object>("Forbidden", false);

            double? yourRatingForOther;
            double? otherRatingForYou;

            if (p.VehicleOwnerId == me)
            {
                yourRatingForOther = p.VehicleOwnerRating;   // أنت صاحب العربية → تقييمك لصاحب الشاحن
                otherRatingForYou = p.ChargerOwnerRating;   // تقييم صاحب الشاحن ليك
            }
            else
            {
                yourRatingForOther = p.ChargerOwnerRating;   // أنت صاحب الشاحن → تقييمك لصاحب العربية
                otherRatingForYou = p.VehicleOwnerRating;   // تقييم صاحب العربية ليك
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

            // ابسط شكل: رجّع آخر عمليات للطرف الحالي
            var items = await _ctx.Set<ProcessEntity>()
                .AsNoTracking()
                .Where(p => p.VehicleOwnerId == me || p.ChargerOwnerId == me)
                .OrderByDescending(p => p.DateCreated)
                .Take(50)
                .Select(p => new {
                    p.Id,
                    p.ChargerRequestId,
                    p.Status,
                    p.AmountCharged,
                    p.AmountPaid,
                    p.DateCreated,
                    p.DateCompleted
                })
                .ToListAsync(ct);

            return new ApiResponse<object>(items, "My activities fetched", true);
        }
        private async Task SendToUserAsync(string userId, string title, string body, int relatedRequestId, string notificationType, CancellationToken ct)
        {
            var tokens = await _ctx.Set<DeviceToken>()
                                   .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Token))
                                   .Select(t => t.Token)
                                   .ToListAsync(ct);

            foreach (var tk in tokens)
            {
                try { await _firebase.SendNotificationAsync(tk, title, body, relatedRequestId, notificationType); }
                catch { /* سجل لو حابب */ }
            }
        }
        private string CurrentUserId()
           => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        public static DateTime GetEgyptTime()
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);
        }




    }
}
