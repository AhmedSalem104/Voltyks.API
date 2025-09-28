using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Data;
using ChargingRequestEntity = Voltyks.Persistence.Entities.Main.ChargingRequest;


namespace Voltyks.Application.Services.ChargingRequest.Interceptor
{
    public sealed class ChargingRequestCleanupInterceptor : SaveChangesInterceptor
    {
        // عدِّل الأسماء حسب الـ DbContext/Entities عندك
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken ct = default)
        {
            var ctx = eventData.Context as VoltyksDbContext;
            if (ctx == null)
                return await base.SavingChangesAsync(eventData, result, ct);

            var now = DateTime.UtcNow;

            // 1) هات الطلبات اللي اتعدّلت في هذه العملية
            var modifiedRequests = ctx.ChangeTracker.Entries<ChargingRequestEntity>()
                .Where(e => e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            if (modifiedRequests.Count == 0)
                return await base.SavingChangesAsync(eventData, result, ct);

            // 2) اجمع المستخدمين المتأثرين (UserId) من الطلبات المعدلة
            var affectedUserIds = modifiedRequests
                .Select(r => r.UserId)
                .Where(uid => !string.IsNullOrWhiteSpace(uid))
                .Distinct()
                .ToList();

            if (affectedUserIds.Count == 0)
                return await base.SavingChangesAsync(eventData, result, ct);

            // 3) لكل مستخدم متأثر، طبِّق منطق التنضيف:
            foreach (var uid in affectedUserIds)
            {

                var requestsToDelete = await ctx.ChargingRequests
                    .Where(c =>
                        c.UserId == uid &&
                        (
                            // الحالة الأولى: عدّى 5 دقائق ولسه Pending
                            (EF.Functions.DateDiffMinute(c.RequestedAt, now) >= 5
                                && string.Equals(c.Status, "pending", StringComparison.OrdinalIgnoreCase))

                            // الحالة الثانية: الحالة = Rejected
                            || string.Equals(c.Status, "rejected", StringComparison.OrdinalIgnoreCase)
                        )
                    ).ToListAsync(ct);


                if (requestsToDelete.Count == 0)
                    continue;

                var requestIds = requestsToDelete.Select(r => r.Id).ToList();

                // 4) هات الإشعارات المرتبطة بالطلبات دي
                var relatedNotifs = await ctx.Notifications
                    .Where(n => n.RelatedRequestId.HasValue
                                && requestIds.Contains(n.RelatedRequestId.Value))
                    .ToListAsync(ct);

                // 5) احذف الإشعارات أولًا لتجنب مشاكل FK (لو مفيش Cascade)
                if (relatedNotifs.Count > 0)
                    ctx.Notifications.RemoveRange(relatedNotifs);

                // 6) احذف الطلبات
                ctx.ChargingRequests.RemoveRange(requestsToDelete);
            }

            // مهم: ما بنعملش SaveChanges هنا؛ بنسيب EF يكمل نفس الـ SaveChanges الأصلي
            return await base.SavingChangesAsync(eventData, result, ct);
        }
    }
}
