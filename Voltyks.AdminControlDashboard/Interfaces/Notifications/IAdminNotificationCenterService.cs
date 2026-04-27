using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voltyks.AdminControlDashboard.Dtos.Notifications;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces.Notifications
{
    /// <summary>
    /// Admin-side operations for the notification center: editing the template
    /// catalogue, sending to a single user, and broadcasting to a segment.
    /// </summary>
    public interface IAdminNotificationCenterService
    {
        // Templates
        Task<ApiResponse<List<NotificationTemplateDto>>> ListTemplatesAsync(CancellationToken ct = default);
        Task<ApiResponse<NotificationTemplateDto>> GetTemplateAsync(string key, CancellationToken ct = default);
        Task<ApiResponse<NotificationTemplateDto>> UpdateTemplateAsync(string key, UpdateNotificationTemplateDto dto, string adminUserId, CancellationToken ct = default);
        Task<ApiResponse<object>> ResetTemplateAsync(string key, CancellationToken ct = default);
        Task<ApiResponse<TemplatePreviewResultDto>> PreviewTemplateAsync(string key, TemplatePreviewRequestDto dto, CancellationToken ct = default);

        // Dispatch
        Task<ApiResponse<object>> SendToUserAsync(SendToUserDto dto, string adminUserId, CancellationToken ct = default);
        Task<ApiResponse<BroadcastResultDto>> BroadcastAsync(BroadcastDto dto, string adminUserId, CancellationToken ct = default);
    }
}
