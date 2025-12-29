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
            string? type = null,
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

                // Filter by type: report, complaint, product_reservation
                if (!string.IsNullOrEmpty(type))
                {
                    var notificationType = type.ToLower() switch
                    {
                        "report" => NotificationTypes.Admin_Report_Created,
                        "complaint" => NotificationTypes.Admin_Complaint_Created,
                        "product_reservation" => NotificationTypes.Admin_Reservation_Created,
                        _ => null
                    };

                    if (notificationType != null)
                    {
                        query = query.Where(n => n.Type == notificationType);
                    }
                }

                var totalCount = await query.CountAsync(ct);

                var notifications = await query
                    .OrderByDescending(n => n.SentAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new AdminNotificationDto
                    {
                        Id = GetNotificationId(n.Type, n.OriginalId),
                        Type = GetNotificationTypeName(n.Type),
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

        private static string GetNotificationId(string? notificationType, int? originalId)
        {
            return notificationType switch
            {
                NotificationTypes.Admin_Report_Created => $"report_{originalId}",
                NotificationTypes.Admin_Complaint_Created => $"complaint_{originalId}",
                NotificationTypes.Admin_Reservation_Created => $"reservation_{originalId}",
                _ => $"unknown_{originalId}"
            };
        }

        private static string GetNotificationTypeName(string? notificationType)
        {
            return notificationType switch
            {
                NotificationTypes.Admin_Report_Created => "report",           // بلاغ
                NotificationTypes.Admin_Complaint_Created => "complaint",     // شكوى
                NotificationTypes.Admin_Reservation_Created => "product_reservation", // حجز منتج
                _ => "unknown"
            };
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
