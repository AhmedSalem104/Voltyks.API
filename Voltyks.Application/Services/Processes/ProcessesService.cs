using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.ChargingRequest;
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

        public ProcessesService(VoltyksDbContext ctx, IHttpContextAccessor http)
        {
            _ctx = ctx; _http = http; 
        }

        private string CurrentUserId()
            => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        public async Task<ApiResponse<object>> ConfirmByVehicleOwnerAsync(ConfirmByVehicleOwnerDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var req = await _ctx.Set<ChargingRequestEntity>()
                .FirstOrDefaultAsync(r => r.Id == dto.ChargerRequestId, ct);
            if (req is null) return new ApiResponse<object>("Charger request not found", false);

            // تأكد إن المنفّذ هو صاحب العربية
            if (req.UserId != me) return new ApiResponse<object>("Forbidden", false);

            // لو已有 Process لنفس الطلب امنع التكرار
            var exists = await _ctx.Set<ProcessEntity>().AnyAsync(p => p.ChargerRequestId == req.Id, ct);
            if (exists) return new ApiResponse<object>("Process already created for this request", false);

            // إنشاء Process + تحديث حالة الطلب
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

                // (اختياري) أضف الـ processId إلى CurrentActivities بعد الحفظ
                await _ctx.SaveChangesAsync(ct);

                // بعد SaveChanges الأول (اللي بيولّد process.Id)
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

            // (اختياري) Notification → لصاحب المحطة بالقيم

            return new ApiResponse<object>(new { processId = process.Id }, "Process created & request moved to PendingCompleted", true);
        }

        public async Task<ApiResponse<object>> OwnerDecisionAsync(OwnerDecisionDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
            if (process is null) return new ApiResponse<object>("Process not found", false);

            // المصرّح: صاحب المحطة فقط
            if (process.ChargerOwnerId != me) return new ApiResponse<object>("Forbidden", false);

            var request = await _ctx.Set<ChargingRequestEntity>().FirstOrDefaultAsync(r => r.Id == process.ChargerRequestId, ct);
            if (request is null) return new ApiResponse<object>("Charger request not found", false);

            var decision = (dto.Decision ?? "Confirm").Trim().ToLowerInvariant();

            using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                if (decision == "confirm")
                {
                    process.Status = ProcessStatus.Completed; // مرحليًا قبل التقييم
                    process.DateCompleted = DateTime.UtcNow;
                    request.Status = "Completed";

                    _ctx.Update(process);
                    _ctx.Update(request);
                    await _ctx.SaveChangesAsync(ct);

                    // (اختياري) سوي الرسوم تلقائيًا
                    // await _feesService.TransferVoltyksFeesAsync(new RequestIdDto { RequestId = process.ChargerRequestId }, ct);
                }
                else
                {
                    process.Status = ProcessStatus.Aborted;
                    request.Status = "Aborted";

                    _ctx.Update(process);
                    _ctx.Update(request);
                    await _ctx.SaveChangesAsync(ct);

                    // ممكن هنا تعمل Rollback لأي آثار أخرى لو عندك
                }

                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>("Failed to apply decision", false, new() { ex.Message });
            }

            // (اختياري) Notifications للطرفين

            var msg = decision == "confirm" ? "Process confirmed" : "Process reported (aborted)";
            return new ApiResponse<object>(new { processId = process.Id, status = process.Status.ToString() }, msg, true);
        }

        public async Task<ApiResponse<object>> SubmitRatingAsync(SubmitRatingDto dto, CancellationToken ct = default)
        {
            var me = CurrentUserId();
            if (string.IsNullOrEmpty(me))
                return new ApiResponse<object>("Unauthorized", false);

            var process = await _ctx.Set<ProcessEntity>().FirstOrDefaultAsync(p => p.Id == dto.ProcessId, ct);
            if (process is null) return new ApiResponse<object>("Process not found", false);

            if (dto.RatingForOther < 1 || dto.RatingForOther > 5)
                return new ApiResponse<object>("Invalid rating value (1..5)", false);

            // حدّد من يقيم مين (زي ما عندك)
            var raterId = me;
            var rateeId = (process.VehicleOwnerId == me) ? process.ChargerOwnerId
                       : (process.ChargerOwnerId == me) ? process.VehicleOwnerId
                       : null;
            if (rateeId is null) return new ApiResponse<object>("Forbidden", false);

            // 1) امنع تكرار التقييم لنفس العملية من نفس الشخص
            var already = await _ctx.Set<RatingsHistory>()
                .AnyAsync(x => x.ProcessId == process.Id && x.RaterUserId == raterId, ct);
            if (already) return new ApiResponse<object>("You already rated this process", false);

            // 2) خزّن التقييم في Process (per-process rating)
            if (me == process.VehicleOwnerId) process.ChargerOwnerRating = dto.RatingForOther;
            else process.VehicleOwnerRating = dto.RatingForOther;

            // 3) خزّله في RatingsHistory
            await _ctx.AddAsync(new RatingsHistory
            {
                ProcessId = process.Id,
                RaterUserId = raterId,
                RateeUserId = rateeId!,
                Stars = dto.RatingForOther
            }, ct);

            // 4) حدّث التقييم العام للمستخدم المُقَيّم
            var ratee = await _ctx.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == rateeId, ct);
            ratee!.Rating = ((ratee.Rating * ratee.RatingCount) + dto.RatingForOther) / (ratee.RatingCount + 1);
            ratee.RatingCount += 1;

            // 5) لو الاتنين قيّموا → كمّل الحالة نهائيًا
            if (process.VehicleOwnerRating.HasValue && process.ChargerOwnerRating.HasValue)
            {
                process.Status = ProcessStatus.Completed;
                process.DateCompleted = DateTime.UtcNow;

                // (اختياري) لو عايز تشيلها من MyActivities:
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


            return new ApiResponse<object>(new
            {
                processId = process.Id,
                processStatus = process.Status.ToString(),
                yourRatingForOther = dto.RatingForOther
            }, "Rating submitted", true);
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
    }
}
