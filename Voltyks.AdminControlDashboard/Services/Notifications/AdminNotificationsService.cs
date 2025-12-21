using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Notifications;
using Voltyks.AdminControlDashboard.Interfaces.Notifications;
using Voltyks.Core.DTOs;
using Voltyks.Core.Enums;
using Voltyks.Persistence.Data;

namespace Voltyks.AdminControlDashboard.Services.Notifications
{
    public class AdminNotificationsService : IAdminNotificationsService
    {
        private readonly VoltyksDbContext _context;

        public AdminNotificationsService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<AdminNotificationDto>>> GetNotificationsAsync(
            int page = 1,
            int pageSize = 20,
            bool? onlyUnread = null,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.Notifications
                    .AsNoTracking()
                    .Where(n => n.IsAdminNotification)
                    .AsQueryable();

                if (onlyUnread == true)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var totalCount = await query.CountAsync(ct);

                var notifications = await query
                    .OrderByDescending(n => n.SentAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new AdminNotificationDto
                    {
                        Id = n.Type == NotificationTypes.Admin_Report_Created
                            ? $"report_{n.OriginalId}"
                            : $"complaint_{n.OriginalId}",
                        Type = n.Type == NotificationTypes.Admin_Report_Created ? "report" : "complaint",
                        OriginalId = n.OriginalId ?? 0,
                        Title = n.Title,
                        Message = n.Body,
                        UserName = null,
                        Timestamp = n.SentAt,
                        IsRead = n.IsRead
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminNotificationDto>>(
                    data: notifications,
                    message: $"Retrieved {notifications.Count} notifications (Total: {totalCount})",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminNotificationDto>>(
                    message: "Failed to retrieve notifications",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync(CancellationToken ct = default)
        {
            try
            {
                var count = await _context.Notifications
                    .AsNoTracking()
                    .Where(n => n.IsAdminNotification && !n.IsRead)
                    .CountAsync(ct);

                return new ApiResponse<int>(
                    data: count,
                    message: $"{count} unread notifications",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>(
                    message: "Failed to get unread count",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<object>> MarkAsReadAsync(int notificationId, CancellationToken ct = default)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.IsAdminNotification, ct);

                if (notification is null)
                    return new ApiResponse<object>("Notification not found", status: false);

                notification.IsRead = true;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { Id = notificationId, IsRead = true },
                    message: "Notification marked as read",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to mark notification as read",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<object>> MarkAllAsReadAsync(CancellationToken ct = default)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.IsAdminNotification && !n.IsRead)
                    .ToListAsync(ct);

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { MarkedCount = unreadNotifications.Count },
                    message: $"{unreadNotifications.Count} notifications marked as read",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to mark all notifications as read",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }
    }
}
