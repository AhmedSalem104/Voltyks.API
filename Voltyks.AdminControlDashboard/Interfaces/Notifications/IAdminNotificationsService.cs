using Voltyks.AdminControlDashboard.Dtos.Notifications;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces.Notifications
{
    public interface IAdminNotificationsService
    {
        Task<ApiResponse<List<AdminNotificationDto>>> GetNotificationsAsync(
            int page = 1,
            int pageSize = 20,
            bool? onlyUnread = null,
            CancellationToken ct = default);

        Task<ApiResponse<int>> GetUnreadCountAsync(CancellationToken ct = default);

        Task<ApiResponse<object>> MarkAsReadAsync(int notificationId, CancellationToken ct = default);

        Task<ApiResponse<object>> MarkAllAsReadAsync(CancellationToken ct = default);
    }
}
