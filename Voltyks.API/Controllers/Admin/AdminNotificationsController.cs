using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/notifications")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminNotificationsController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/notifications - Get admin notifications with pagination
        /// Filter by type: report (بلاغ), complaint (شكوى), product_reservation (حجز منتج)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? onlyUnread = null,
            [FromQuery] string? type = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminNotificationsService.GetNotificationsAsync(page, pageSize, onlyUnread, type, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/notifications/unread-count - Get count of unread notifications
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminNotificationsService.GetUnreadCountAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// PATCH /api/admin/notifications/{id}/read - Mark a notification as read
        /// </summary>
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminNotificationsService.MarkAsReadAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// PATCH /api/admin/notifications/mark-all-read - Mark all notifications as read
        /// </summary>
        [HttpPatch("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminNotificationsService.MarkAllAsReadAsync(ct);
            return Ok(result);
        }
    }
}
