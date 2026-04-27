using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Notifications;

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

        private string AdminUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

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

        // ──────────────────────────────────────────────────────────────────
        // Notification Templates (catalogue)
        // ──────────────────────────────────────────────────────────────────

        [HttpGet("templates")]
        public async Task<IActionResult> ListTemplates(CancellationToken ct = default)
            => Ok(await _adminServiceManager.AdminNotificationCenterService.ListTemplatesAsync(ct));

        [HttpGet("templates/{key}")]
        public async Task<IActionResult> GetTemplate(string key, CancellationToken ct = default)
            => Ok(await _adminServiceManager.AdminNotificationCenterService.GetTemplateAsync(key, ct));

        [HttpPut("templates/{key}")]
        public async Task<IActionResult> UpdateTemplate(string key, [FromBody] UpdateNotificationTemplateDto dto, CancellationToken ct = default)
            => Ok(await _adminServiceManager.AdminNotificationCenterService.UpdateTemplateAsync(key, dto, AdminUserId, ct));

        [HttpDelete("templates/{key}")]
        public async Task<IActionResult> ResetTemplate(string key, CancellationToken ct = default)
            => Ok(await _adminServiceManager.AdminNotificationCenterService.ResetTemplateAsync(key, ct));

        [HttpPost("templates/{key}/preview")]
        public async Task<IActionResult> PreviewTemplate(string key, [FromBody] TemplatePreviewRequestDto dto, CancellationToken ct = default)
            => Ok(await _adminServiceManager.AdminNotificationCenterService.PreviewTemplateAsync(key, dto, ct));

        // ──────────────────────────────────────────────────────────────────
        // Dispatch
        // ──────────────────────────────────────────────────────────────────

        [HttpPost("send-to-user")]
        public async Task<IActionResult> SendToUser([FromBody] SendToUserDto dto, CancellationToken ct = default)
            => Ok(await _adminServiceManager.AdminNotificationCenterService.SendToUserAsync(dto, AdminUserId, ct));

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastDto dto, CancellationToken ct = default)
            => Ok(await _adminServiceManager.AdminNotificationCenterService.BroadcastAsync(dto, AdminUserId, ct));
    }
}
